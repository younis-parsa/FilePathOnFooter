// FilePathOnFooter - shows the current document's full file path in the editor's bottom margin.
// Copyright (C) 2026 younis-parsa
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace FilePathOnFooter
{
    /// <summary>
    /// MEF export that places <see cref="FilePathBottomMargin"/> into the editor's bottom margin
    /// container, below the horizontal scroll bar.
    /// </summary>
    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name(FilePathBottomMargin.MarginName)]
    [MarginContainer(PredefinedMarginNames.Bottom)]
    [Order(After = PredefinedMarginNames.HorizontalScrollBar)]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class FilePathBottomMarginProvider : IWpfTextViewMarginProvider
    {
        /// <summary>Maps a text buffer to the <see cref="ITextDocument"/> that supplies FilePath.</summary>
        [Import]
        internal ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            return new FilePathBottomMargin(wpfTextViewHost.TextView, TextDocumentFactoryService);
        }
    }
}
