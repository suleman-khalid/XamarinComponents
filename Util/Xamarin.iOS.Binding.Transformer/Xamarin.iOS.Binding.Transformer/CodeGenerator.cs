﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Xamarin.iOS.Binding.Transformer
{
    public static class CodeGenerator
    {
        public async static Task GenerateAsync(ApiDefinition api, string outputFilename)
        {
            //create the base complation unit
            var syntaxFactory = SyntaxFactory.CompilationUnit();


            //added the usings
            foreach (var aUsing in api.Usings)
            {
                var node = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(" " + aUsing.Name));
                node = node.WithTrailingTrivia(SyntaxFactory.LineFeed);

                syntaxFactory = syntaxFactory.AddUsings(node);
            }

            //create the namespace root element
            var @namespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(api.Namespace)).NormalizeWhitespace();
            @namespace = @namespace.WithLeadingTrivia(SyntaxFactory.LineFeed);

            //interate through the delegates
            foreach (var aDel in api.Delegates)
            {
                var delegateDeclaration = SyntaxFactory.DelegateDeclaration(SyntaxFactory.ParseTypeName(" " + aDel.ReturnType), SyntaxFactory.ParseToken(" " + aDel.Name));
                delegateDeclaration = delegateDeclaration.WithLeadingTrivia(SyntaxFactory.LineFeed, SyntaxFactory.Tab);
                delegateDeclaration = delegateDeclaration.WithTrailingTrivia(SyntaxFactory.LineFeed);

                //needs to build parameters
                @namespace = @namespace.AddMembers(delegateDeclaration);
            }

            //interate through the types
            foreach (var aClass in api.Types)
            {
                //build the attributes for the class
                var attributeList = BuildClassAttributes(aClass);

                //create the interface
                var interfaceDeclaration = SyntaxFactory.InterfaceDeclaration(" " + aClass.Name);

                //does the class inherit from anything
                if (aClass.InheritsFrom.Count > 0)
                {
                    //build the baselistsyntax object
                    var baseClasses = BuildClassBaseTypes(aClass);

                    //if it returns items add it to the declaration
                    if (baseClasses.Types.Count > 0)
                        interfaceDeclaration = interfaceDeclaration.WithBaseList(baseClasses);
                }

                 //if there are attributes then add them to the class definition
                if (attributeList.Count > 0)
                {
                    interfaceDeclaration = interfaceDeclaration.WithAttributeLists(attributeList);


                }

                interfaceDeclaration = interfaceDeclaration.WithLeadingTrivia(SyntaxFactory.LineFeed, SyntaxFactory.Tab);
                interfaceDeclaration = interfaceDeclaration.WithTrailingTrivia(SyntaxFactory.LineFeed);

                @namespace = @namespace.AddMembers(interfaceDeclaration);
            }

            //add the namespace to the compilation unit
            syntaxFactory = syntaxFactory.AddMembers(@namespace);


            //format and export as a string
            var code = syntaxFactory
                .NormalizeWhitespace()
                .ToFullString();

            //write to the output file
            using (var writeFile = new StreamWriter(outputFilename))
            {
                await writeFile.WriteAsync(code);
            }

            //Console.WriteLine(code);    


        }

        #region Private Methods

        /// <summary>
        /// Build the base classes/interfaces
        /// </summary>
        /// <param name="aClass"></param>
        /// <returns></returns>
        private static BaseListSyntax BuildClassBaseTypes(ApiClass aClass)
        {

            var tokens = new List<SyntaxNodeOrToken>();

            foreach (var basetype in aClass.InheritsFrom)
            {
                tokens.Add(SyntaxFactory.SimpleBaseType
                    (
                        SyntaxFactory.IdentifierName(basetype.Name)
                    ));


                if (basetype != aClass.InheritsFrom.Last())
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));

            }
        
            return SyntaxFactory.BaseList(SyntaxFactory.SeparatedList<BaseTypeSyntax>(tokens));

        }

        /// <summary>
        /// Build the class attributes
        /// </summary>
        /// <param name="apiClass"></param>
        /// <returns></returns>
        private static SyntaxList<AttributeListSyntax> BuildClassAttributes(ApiClass apiClass)
        {

            var attribs = SyntaxFactory.List<AttributeListSyntax>();

            if (apiClass.IsProtocol)
            {
                var attribList = SyntaxFactory.AttributeList
                            (
                                SyntaxFactory.SingletonSeparatedList<AttributeSyntax>
                                (
                                    SyntaxFactory.Attribute
                                    (
                                        SyntaxFactory.IdentifierName("Protocol")
                                    )
                                )
                            );

                attribs = attribs.Add(attribList);

            }

            if (apiClass.IsCategory)
            {
                var attribList = SyntaxFactory.AttributeList
                           (
                               SyntaxFactory.SingletonSeparatedList<AttributeSyntax>
                               (
                                   SyntaxFactory.Attribute
                                   (
                                       SyntaxFactory.IdentifierName("Category")
                                   )
                               )
                           );

                attribs = attribs.Add(attribList);


            }

            

            if (apiClass.Model != null)
            {

            }

            if (apiClass.BaseType != null)
            {
                var atrs = SyntaxFactory.AttributeList
                            (
                                SyntaxFactory.SingletonSeparatedList<AttributeSyntax>
                                (
                                    SyntaxFactory.Attribute
                                    (
                                        SyntaxFactory.IdentifierName("BaseType")
                                    )
                                    .WithArgumentList
                                    (
                                        SyntaxFactory.AttributeArgumentList
                                        (
                                            SyntaxFactory.SingletonSeparatedList<AttributeArgumentSyntax>
                                            (
                                                SyntaxFactory.AttributeArgument
                                                (
                                                    SyntaxFactory.TypeOfExpression
                                                    (
                                                        SyntaxFactory.IdentifierName(apiClass.BaseType.TypeName)
                                                    )
                                                )
                                            )
                                        )
                                    )
                                )
                            );

                attribs = attribs.Add(atrs);



            }

            if (apiClass.DisableDefaultCtor)
            {
                var attribList = SyntaxFactory.AttributeList
                           (
                               SyntaxFactory.SingletonSeparatedList<AttributeSyntax>
                               (
                                   SyntaxFactory.Attribute
                                   (
                                       SyntaxFactory.IdentifierName("DisableDefaultCtor")
                                   )
                               )
                           );

                attribs = attribs.Add(attribList);


            }

            return attribs;
        }

        #endregion
    }
}
