﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.CodeAnalysis.Razor
{
    internal class DefaultTagHelperResolver : TagHelperResolver
    {
        public DefaultTagHelperResolver(bool designTime)
        {
            DesignTime = designTime;
        }

        public bool DesignTime { get; }

        public override IReadOnlyList<TagHelperDescriptor> GetTagHelpers(Compilation compilation)
        {
            var results = new List<TagHelperDescriptor>();
            var errors = new ErrorSink();

            VisitTagHelpers(compilation, results, errors);
            VisitViewComponents(compilation, results, errors);

            return results;
        }

        private void VisitTagHelpers(Compilation compilation, List<TagHelperDescriptor> results, ErrorSink errors)
        {
            var types = new List<INamedTypeSymbol>();
            var visitor = TagHelperTypeVisitor.Create(compilation, types);

            VisitCompilation(visitor, compilation);

            var factory = new DefaultTagHelperDescriptorFactory(compilation, DesignTime);

            foreach (var type in types)
            {
                var descriptors = factory.CreateDescriptors(type, errors);
                results.AddRange(descriptors);
            }
        }

        private void VisitViewComponents(Compilation compilation, List<TagHelperDescriptor> results, ErrorSink errors)
        {
            var types = new List<INamedTypeSymbol>();
            var visitor = ViewComponentTypeVisitor.Create(compilation, types);

            VisitCompilation(visitor, compilation);

            var factory = new ViewComponentTagHelperDescriptorFactory(compilation);

            foreach (var type in types)
            {
                try
                {
                    var descriptor = factory.CreateDescriptor(type);

                    results.Add(descriptor);
                }
                catch (Exception ex)
                {
                    errors.OnError(SourceLocation.Zero, ex.Message, length: 0);
                }
            }
        }

        private static void VisitCompilation(SymbolVisitor visitor, Compilation compilation)
        {
            visitor.Visit(compilation.Assembly.GlobalNamespace);

            foreach (var reference in compilation.References)
            {
                if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
                {
                    visitor.Visit(assembly.GlobalNamespace);
                }
            }
        }
    }
}