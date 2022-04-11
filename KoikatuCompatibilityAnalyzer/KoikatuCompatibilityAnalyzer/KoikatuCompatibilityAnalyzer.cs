using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KoikatuCompatibilityAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class KoikatuCompatibilityAnalyzer : DiagnosticAnalyzer
    {
        #region Defines

        private const string Description = "To maintain compatibility with other game versions you should access it by reflection (Traverse, AccessTools) and handle the case where the member doesn't exist. Trying to access members that don't exist or have a different signature will throw an exception and in some cases might even crash the game.";
        private const string Category = "KK.Compatibility";

        private static readonly DiagnosticDescriptor _ruleAsMissingMembers = new DiagnosticDescriptor("KKANAL01",
            "Member is missing or has a different signature in games without Darkness.",
            "Member {0} is missing or has a different signature in games without Darkness.",
            Category, DiagnosticSeverity.Warning, true, Description);
        private static readonly DiagnosticDescriptor _ruleAsMissingTypes = new DiagnosticDescriptor("KKANAL02",
            "Type is missing in games without Darkness.",
            "Type {0} is missing in games without Darkness.",
            Category, DiagnosticSeverity.Warning, true, Description);
        private static readonly DiagnosticDescriptor _ruleKkpMissingMembers = new DiagnosticDescriptor("KKANAL03",
            "Member is missing or has a different signature in KK Party.",
            "Member {0} is missing or has a different signature in KK Party.",
            Category, DiagnosticSeverity.Warning, true, Description);
        private static readonly DiagnosticDescriptor _ruleKkpMissingTypes = new DiagnosticDescriptor("KKANAL04",
            "Type is missing in KK Party.",
            "Type {0} is missing in KK Party.",
            Category, DiagnosticSeverity.Warning, true, Description);
        
        private static readonly DiagnosticDescriptor _ruleAsDifferentConstants = new DiagnosticDescriptor("KKANAL05",
            "Value of this constant is different in games without Darkness.",
            "Value of {0} is different in games without Darkness. If you compile while referencing this constant, the value from the referenced dll will be burned into your plugin, it will not be read from the game DLL so you might get unexpected behavior on different game installs. WARNING: If the constant is an enum value, its corresponding number will be saved and not the actual enum value, so in different game installs it will end up as a completely different enum value!",
            Category, DiagnosticSeverity.Warning, true, Description);
        
        private static readonly DiagnosticDescriptor _ruleKkpDifferentConstants = new DiagnosticDescriptor("KKANAL06",
            "Value of this constant is different in KK Party.",
            "Value of {0} is different in KK Party. If you compile while referencing this constant, the value from the referenced dll will be burned into your plugin, it will not be read from the game DLL so you might get unexpected behavior on different game installs. WARNING: If the constant is an enum value, its corresponding number will be saved and not the actual enum value, so in different game installs it will end up as a completely different enum value!",
            Category, DiagnosticSeverity.Warning, true, Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(_ruleAsMissingMembers, _ruleAsMissingTypes, _ruleKkpMissingMembers, _ruleKkpMissingTypes, _ruleAsDifferentConstants, _ruleKkpDifferentConstants);

        private static readonly HashSet<string> _asMissingMembers = LoadResource(Resources.asMissingMembers);
        private static readonly HashSet<string> _asMissingTypes = LoadResource(Resources.asMissingTypes);
        private static readonly HashSet<string> _kkpMissingMembers = LoadResource(Resources.kkpMissingMembers);
        private static readonly HashSet<string> _kkpMissingTypes = LoadResource(Resources.kkpMissingTypes);
        private static readonly HashSet<string> _asDifferentConstants = LoadResource(Resources.asDifferentConstants);
        private static readonly HashSet<string> _kkpDifferentConstants = LoadResource(Resources.kkpDifferentConstants);

        private static HashSet<string> LoadResource(string missingMembers)
        {
            var asMissingMembers = new HashSet<string>();
            // Need to split the resource into lines, this is the most efficient way of doing it, probably
            using (var reader = new StringReader(missingMembers))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    asMissingMembers.Add(line);
            }
            return asMissingMembers;
        }

        #endregion

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(NodeAction, SyntaxKind.IdentifierName);
        }

        private static void NodeAction(SyntaxNodeAnalysisContext obj)
        {
            var node = obj.Node;

            // Try to get the actual member this node is referring to
            var symbolInfo = obj.SemanticModel.GetSymbolInfo(node, obj.CancellationToken);
            var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
            if (symbol == null) return;

            // If inside of nameof then don't show warnings because nameof is compiled to a string so nothing is actually referenced (and it would be annoying in reflection)
            if (IsInsideNameof(node)) return;

            // Create a dot separated full path to the member/type to be compared with the lists generated by assembly comparer tool
            var str = symbol.ContainingType != null ? symbol.ContainingType.ToDisplayString() + "." + symbol.Name : symbol.ToDisplayString();

            if (_asMissingMembers.Contains(str))
                obj.ReportDiagnostic(Diagnostic.Create(_ruleAsMissingMembers, node.GetLocation(), symbol.Name));
            if (_asMissingTypes.Contains(str))
                obj.ReportDiagnostic(Diagnostic.Create(_ruleAsMissingTypes, node.GetLocation(), symbol.Name));
            if (_kkpMissingMembers.Contains(str))
                obj.ReportDiagnostic(Diagnostic.Create(_ruleKkpMissingMembers, node.GetLocation(), symbol.Name));
            if (_kkpMissingTypes.Contains(str))
                obj.ReportDiagnostic(Diagnostic.Create(_ruleKkpMissingTypes, node.GetLocation(), symbol.Name));

            if (_asDifferentConstants.Contains(str))
                obj.ReportDiagnostic(Diagnostic.Create(_ruleAsDifferentConstants, node.GetLocation(), symbol.Name));
            if (_kkpDifferentConstants.Contains(str))
                obj.ReportDiagnostic(Diagnostic.Create(_ruleKkpDifferentConstants, node.GetLocation(), symbol.Name));
        }

        private static bool IsInsideNameof(SyntaxNode node)
        {
            var currentNode = node;
            while (currentNode != null)
            {
                switch (currentNode.Kind())
                {
                    case SyntaxKind.Block: // Block always marks the end of current statement
                        return false;
                    case SyntaxKind.InvocationExpression when ((InvocationExpressionSyntax)currentNode).Expression.ToString() == "nameof":
                        return true;
                    case SyntaxKind.InvocationExpression:
                        return false;
                    default:
                        currentNode = currentNode.Parent;
                        continue;
                }
            }
            return false;
        }
    }
}
