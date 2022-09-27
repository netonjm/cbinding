//
// DataProvider.cs
//
// Authors:
//   Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// Copyright (C) 2007 Marcos David Marin Amador
//
//
// This source code is licenced under The MIT License:
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Collections.Generic;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using ClangSharp;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text.Classification;

namespace CBinding.Parser
{
	//public class ParameterDataProvider : MonoDevelop.Ide.CodeCompletion.ParameterHintingResult
	//{
	//	List<OverloadCandidate> ParameterInformation { get; }

	//	public ParameterDataProvider (int startOffset, List<OverloadCandidate> parameterInformation) : base (startOffset)
	//	{
	//		ParameterInformation = parameterInformation;
	//		foreach (var pi in ParameterInformation) {
	//			data.Add (new DataWrapper (pi));
	//		}
	//	}
	//}

	class ClangCompletionCategory
	{
		public static string FunctionCategory = "Function";
		public static string NamespaceCategory = "Namespace";
		public static string FunctionTemplateCategory = "Function template";
		public static string MethodCategory = "Class method";
		public static string ClassCategory = "Class";
		public static string ClassTemplateCategory = "Class template";
		public static string ClassTemplatePartialCategory = "Class template partial specialization";
		public static string FieldCategory = "Field";
		public static string StructCategory = "Struct";
		public static string EnumerationCategory = "Enumeration";
		public static string EnumeratorCategory = "Enumerator";
		public static string UnionCategory = "Union";
		public static string TypedefCategory = "Typedef";
		public static string VariablesCategory = "Variable";
		public static string ParameterCategory = "Parameter";
		public static string MacroCategory = "Macro";
		public static string OtherCategory = "Other";
        private string classTemplatePartialCategory;

        public ClangCompletionCategory (string classTemplatePartialCategory)
        {
            this.classTemplatePartialCategory = classTemplatePartialCategory;
        }
	}

	public class CompletionData
	{
		IconId image;
		string text;
		string description;
		string completionString;
		ClangCompletionCategory CompletionCategory;

        public virtual int CompareTo(object obj)
        {
			return obj == this ? 1 : 0;
		}

		public CompletionData(CXCompletionResult item, string dataString)
		{
			switch (item.CursorKind)
			{
				case CXCursorKind.ClassDecl:
					image = Stock.Class;
					CompletionCategory = new ClangCompletionCategory(ClangCompletionCategory.ClassCategory);
					break;
				case CXCursorKind.ClassTemplate:
					image = Stock.Class;
					CompletionCategory = new ClangCompletionCategory(ClangCompletionCategory.ClassTemplateCategory);
					break;
				case CXCursorKind.ClassTemplatePartialSpecialization:
					image = Stock.Class;
					CompletionCategory = new ClangCompletionCategory(ClangCompletionCategory.ClassTemplatePartialCategory);
					break;
				case CXCursorKind.StructDecl:
					image = Stock.Struct;
					CompletionCategory = new ClangCompletionCategory(ClangCompletionCategory.StructCategory);
					break;
				case CXCursorKind.UnionDecl:
					image = "md-union";
					CompletionCategory = new ClangCompletionCategory(ClangCompletionCategory.UnionCategory);
					break;
				case CXCursorKind.EnumDecl:
					image = Stock.Enum;
					CompletionCategory = new ClangCompletionCategory(ClangCompletionCategory.EnumerationCategory);
					break;
				case CXCursorKind.EnumConstantDecl:
					image = Stock.Literal;
					CompletionCategory = new ClangCompletionCategory(ClangCompletionCategory.EnumeratorCategory);
					break;
				case CXCursorKind.FunctionDecl:
					image = Stock.Method;
					CompletionCategory = new ClangCompletionCategory(ClangCompletionCategory.FunctionCategory);
					break;
				case CXCursorKind.FunctionTemplate:
					image = Stock.Method;
					CompletionCategory = new ClangCompletionCategory(ClangCompletionCategory.FunctionTemplateCategory);
					break;
				case CXCursorKind.Namespace:
					image = Stock.NameSpace;
					CompletionCategory = new ClangCompletionCategory(ClangCompletionCategory.NamespaceCategory);
					break;
				case CXCursorKind.TypedefDecl:
					image = Stock.Interface;
					CompletionCategory = new ClangCompletionCategory(ClangCompletionCategory.TypedefCategory);
					break;
				case CXCursorKind.CXXMethod:
					image = Stock.Field;
					CompletionCategory = new ClangCompletionCategory(ClangCompletionCategory.MethodCategory);
					break;
				case CXCursorKind.FieldDecl:
					image = Stock.Field;
					CompletionCategory = new ClangCompletionCategory(ClangCompletionCategory.FieldCategory);
					break;
				case CXCursorKind.VarDecl:
					image = Stock.Field;
					CompletionCategory = new ClangCompletionCategory(ClangCompletionCategory.VariablesCategory);
					break;
				case CXCursorKind.MacroDefinition:
					image = Stock.Literal;
					CompletionCategory = new ClangCompletionCategory(ClangCompletionCategory.MacroCategory);
					break;
				case CXCursorKind.ParmDecl:
					image = Stock.Field;
					CompletionCategory = new ClangCompletionCategory(ClangCompletionCategory.ParameterCategory);
					break;
				default:
					image = Stock.Literal;
					CompletionCategory = new ClangCompletionCategory(ClangCompletionCategory.OtherCategory);
					break;
			}
			text = dataString;
			completionString = dataString;
			description = string.Empty;
		}

		public IconId Icon
		{
			get { return image; }
		}

		public string DisplayText
		{
			get { return text; }
		}

		public string Description
		{
			get { return description; }
		}

		public string CompletionText
		{
			get { return completionString; }
		}
	}
}
