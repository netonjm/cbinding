
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using System.Collections.Immutable;
using MonoDevelop.Ide.Gui.Documents;
using Monodoc;
using ClangSharp;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CBinding.Parser;
using Gtk;

namespace CBinding
{
    [Export(typeof(IAsyncCompletionSourceProvider))]
	[ContentType(CContentTypeDefinition.CContentType)]
	[Name("Hello World completion item source")]
	internal class CCompletionSourceProvider : IAsyncCompletionSourceProvider
	{
		private readonly ITextStructureNavigatorSelectorService textStructureNavigatorSelectorService;

		[ImportingConstructor]
		public CCompletionSourceProvider(
		   ITextStructureNavigatorSelectorService textStructureNavigatorSelectorService
		   )
		{
			this.textStructureNavigatorSelectorService = textStructureNavigatorSelectorService ??
				throw new ArgumentNullException(nameof(textStructureNavigatorSelectorService));
		}

		public IAsyncCompletionSource GetOrCreate(ITextView textView)
			=> textView.Properties.GetOrCreateSingletonProperty(
				typeof(CCompletionSourceProvider),
				() => this.CreateAndConfigureCompletionSource(textView));

		private IAsyncCompletionSource CreateAndConfigureCompletionSource(ITextView textView)
		{
			var source = new CCompletionSource(textStructureNavigatorSelectorService, textView);
			return source;
		}
	}

	class CCompletionSource : IAsyncCompletionSource
	{
		private static ImageElement CompletionItemIcon = new ImageElement(new ImageId(new Guid("ae27a6b0-e345-4288-96df-5eaf394ee369"), 3335), "Hello Icon");

		private ImmutableArray<CompletionItem> sampleItems;
		ITextStructureNavigatorSelectorService textStructureNavigatorSelectorService;

		ITextView textView;
		public CCompletionSource(ITextStructureNavigatorSelectorService textStructureNavigatorSelectorService, ITextView textView)
		{
			this.textStructureNavigatorSelectorService = textStructureNavigatorSelectorService;
			this.textView = textView;

		}

		public CompletionStartData InitializeCompletion(CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token)
		{
			if (!char.IsLetterOrDigit(trigger.Character))
				return CompletionStartData.DoesNotParticipateInCompletion;

			var navigator = this.textStructureNavigatorSelectorService.GetTextStructureNavigator(triggerLocation.Snapshot.TextBuffer);
			var extent = navigator.GetExtentOfWord(triggerLocation - 1);

			if (extent.IsSignificant)
			{
				var extentText = extent.Span.GetText();
				if (extentText.Length == 1 && !char.IsLetterOrDigit(extentText[0]))
				{
					return new CompletionStartData(CompletionParticipation.ProvidesItems, new SnapshotSpan(triggerLocation, triggerLocation));
				}

				return new CompletionStartData(CompletionParticipation.ProvidesItems, extent.Span);
			}

			return new CompletionStartData(CompletionParticipation.ProvidesItems, new SnapshotSpan(triggerLocation, 0));
		}

		char previous = ' ';
		List<CXUnsavedFile> unsavedFiles;
		static string operatorPattern = "operator\\s*(\\+|\\-|\\*|\\/|\\%|\\^|\\&|\\||\\~|\\!|\\=|\\<|\\>|\\(\\s*\\)|\\[\\s*\\]|new|delete)";
		static Regex operatorFilter = new Regex(operatorPattern, RegexOptions.Compiled);

		/// <summary>
		/// Allowed chars to be next to an identifier
		/// </summary>
		static char[] allowedChars = new char[] {
					'.', ':', ' ', '\t', '=', '*', '+', '-', '/', '%', ',', '&',
					'|', '^', '{', '}', '[', ']', '(', ')', '\n', '!', '?', '<', '>'
				};

		public async Task<CompletionContext> GetCompletionContextAsync(IAsyncCompletionSession session, CompletionTrigger trigger, SnapshotPoint triggerLocation, SnapshotSpan applicableToSpan, CancellationToken token)
		{
			var type = typeof(MonoDevelop.Ide.Gui.Documents.DocumentController);
			var documentController = (DocumentController)textView.Properties[type];
			var project = (CProject)documentController.Owner;

			List<ClangCompletionUnit> list = new List<ClangCompletionUnit>();

			var position = textView.Caret.Position.BufferPosition;
			var textBuffer = textView.TextBuffer;
			var snapshot = textBuffer.CurrentSnapshot;

			var line = snapshot.GetLineFromPosition(position);
			var column = position.Position - line.Start.Position;

			var unsavedFiles = project.UnsavedFiles.Get();
			IntPtr pResults = project.ClangManager.CodeComplete(line.LineNumber, column, unsavedFiles.ToArray(), documentController.Document.FilePath);

			var completionChar = ' '; //triggerInfo.TriggerCharacter ?? ' ';
			bool fieldOrMethodMode = completionChar == '.' || completionChar == '>' ? true : false;

			if (pResults.ToInt64() != 0)
			{
				CXCodeCompleteResults results = Marshal.PtrToStructure<CXCodeCompleteResults>(pResults);
				for (int i = 0; i < results.NumResults; i++)
				{
					IntPtr iteratingPointer = results.Results + i * Marshal.SizeOf<CXCompletionResult>();
					CXCompletionResult resultItem = Marshal.PtrToStructure<CXCompletionResult>(iteratingPointer);

					foreach (var cd in GetCompletionUnits(resultItem, operatorFilter, fieldOrMethodMode))
						list.Add(cd);
				}
			}

			sampleItems = ImmutableArray.Create(
				new CompletionItem("include", this, CompletionItemIcon),

				new CompletionItem("int", this, CompletionItemIcon),
				new CompletionItem("double", this, CompletionItemIcon),
				new CompletionItem("float", this, CompletionItemIcon),
				new CompletionItem("char", this, CompletionItemIcon),

				new CompletionItem("scanf", this, CompletionItemIcon),

				new CompletionItem("else", this, CompletionItemIcon),
				new CompletionItem("for", this, CompletionItemIcon),
				new CompletionItem("printf", this, CompletionItemIcon),
				new CompletionItem("return", this, CompletionItemIcon)
				);

			var items = ImmutableArray.CreateBuilder<CompletionItem>();
			items.AddRange(sampleItems);
			var result = new CompletionContext(items.ToImmutable());
			return await Task.FromResult(result);
		}

		// modified code of Michael Hutchinson from https://github.com/mhutch/cbinding/pull/1#discussion_r34485216
		IEnumerable<ClangCompletionUnit> GetCompletionUnits(CXCompletionResult resultItem, Regex operatorFilter, bool fieldOrMethodMode)
		{
			var completionString = new CXCompletionString(resultItem.CompletionString);
			uint completionchunknum = clang.getNumCompletionChunks(completionString.Pointer);
			for (uint j = 0; j < completionchunknum; j++)
			{
				if (clang.getCompletionChunkKind(completionString.Pointer, j) != CXCompletionChunkKind.TypedText)
					continue;
				switch (resultItem.CursorKind)
				{
					case CXCursorKind.Destructor:
					case CXCursorKind.UnaryOperator:
					case CXCursorKind.BinaryOperator:
					case CXCursorKind.CompoundAssignOperator:
						continue;
				}
				if (fieldOrMethodMode)
					switch (resultItem.CursorKind)
					{
						case CXCursorKind.ClassDecl:
						case CXCursorKind.StructDecl:
							continue;
					}
				string realstring = clang.getCompletionChunkText(completionString.Pointer, j).ToString();
				if (operatorFilter.IsMatch(realstring))
					continue;
				uint priority = clang.getCompletionPriority(completionString.Pointer);
				yield return new ClangCompletionUnit(resultItem, realstring, priority);
			}
		}

		public async Task<object> GetDescriptionAsync(IAsyncCompletionSession session, CompletionItem item, CancellationToken token)
		{
			var content = new ContainerElement(
			ContainerElementStyle.Wrapped,
			CompletionItemIcon,
			new ClassifiedTextElement(
				new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, item.DisplayText)));

			var result = new ContainerElement(
				ContainerElementStyle.Stacked,
				content);

			return await Task.FromResult(result);
		}
	}

}