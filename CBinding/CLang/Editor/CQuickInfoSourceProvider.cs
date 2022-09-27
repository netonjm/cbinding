
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Editor;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Adornments;

namespace CBinding
{
	sealed class CAsyncQuickInfoSource : IAsyncQuickInfoSource
	{
		// Copied from KnownMonikers, because Mono doesn't support ImageMoniker type.
		private static readonly ImageId AssemblyWarningImageId = new ImageId(
			new Guid("{ae27a6b0-e345-4288-96df-5eaf394ee369}"),
			200);

		private ITextBuffer textBuffer;

		public CAsyncQuickInfoSource(ITextBuffer textBuffer)
		{
			this.textBuffer = textBuffer;
		}

		public void Dispose()
		{
			// This provider does not perform any cleanup.
		}

		public Task<QuickInfoItem> GetQuickInfoItemAsync(
			IAsyncQuickInfoSession session,
			CancellationToken cancellationToken)
		{
			var triggerPoint = session.GetTriggerPoint(this.textBuffer.CurrentSnapshot);
			if (triggerPoint != null)
			{
				var line = triggerPoint.Value.GetContainingLine();
				var lineNumber = triggerPoint.Value.GetContainingLine().LineNumber;
				var lineSpan = this.textBuffer.CurrentSnapshot.CreateTrackingSpan(
					line.Extent,
					SpanTrackingMode.EdgeInclusive);

				object content = null;

				// Check if this is an even line.
				if ((lineNumber % 2) == 1)
				{
					content = new ContainerElement(
						ContainerElementStyle.Wrapped,
						new ImageElement(AssemblyWarningImageId),
						new ClassifiedTextElement(
							new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, "Even Or Odd: "),
							new ClassifiedTextRun(PredefinedClassificationTypeNames.Identifier, "Even")));
				}
				else
				{
					content = new ContainerElement(
						ContainerElementStyle.Wrapped,
						new ImageElement(AssemblyWarningImageId),
						new ClassifiedTextElement(
							new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, "Even Or Odd: "),
							new ClassifiedTextRun(PredefinedClassificationTypeNames.Identifier, "Odd")));
				}

				var contentContainer = new ContainerElement(
					ContainerElementStyle.Stacked,
					content,
					new ClassifiedTextElement(
						new ClassifiedTextRun(
							PredefinedClassificationTypeNames.Identifier,
							"The current date and time is: " + DateTime.Now.ToString())));

				return Task.FromResult(
					new QuickInfoItem(
						lineSpan,
						contentContainer));
			}

			return Task.FromResult<QuickInfoItem>(null);
		}
	}

	[System.Composition.Export (typeof(IAsyncQuickInfoSourceProvider))]
	[Name("Even Line Async Quick Info Provider")]
	[ContentType("any")]
	[Order]
	internal sealed class CQuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
	{
		public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
		{
			return new CAsyncQuickInfoSource(textBuffer);
		}
	}

}