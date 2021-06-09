﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KoikatuDarknessCompatibilityAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class KoikatuDarknessCompatibilityAnalyzerAnalyzer : DiagnosticAnalyzer
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

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(_ruleAsMissingMembers, _ruleAsMissingTypes, _ruleKkpMissingMembers, _ruleKkpMissingTypes);

        private static readonly HashSet<string> _asMissingMembers = LoadResource(Resources.asMissingMembers);
        private static readonly HashSet<string> _asMissingTypes = LoadResource(Resources.asMissingTypes);
        private static readonly HashSet<string> _kkpMissingMembers = LoadResource(Resources.kkpMissingMembers);
        private static readonly HashSet<string> _kkpMissingTypes = LoadResource(Resources.kkpMissingTypes);

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
