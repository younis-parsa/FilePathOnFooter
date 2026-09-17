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

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;

namespace FilePathOnFooter
{
    /// <summary>
    /// A real editor margin (not the VS status bar, not an adornment) that is docked into the
    /// bottom margin container of the editor and renders the current document's full path in a
    /// read-only <see cref="TextBox"/>, so the text can be selected and copied with Ctrl+C.
    /// </summary>
    internal sealed class FilePathBottomMargin : Border, IWpfTextViewMargin
    {
        /// <summary>Name used to register the margin and to look it up via <see cref="GetTextViewMargin"/>.</summary>
        public const string MarginName = "FilePathBottomMargin";

        private const string NoFileText = "<no file on disk>";

        private readonly IWpfTextView _textView;
        private readonly ITextDocumentFactoryService _documentFactory;
        private readonly TextBox _pathBox;

        /// <summary>Tracks UI-thread marshalling so pending updates are owned, not fire-and-forget.</summary>
        private readonly JoinableTaskCollection _pendingUpdates;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        private ITextDocument _document;
        private bool _isDisposed;

        internal FilePathBottomMargin(IWpfTextView textView, ITextDocumentFactoryService documentFactory)
        {
            if (textView == null) throw new ArgumentNullException(nameof(textView));
            if (documentFactory == null) throw new ArgumentNullException(nameof(documentFactory));

            _textView = textView;
            _documentFactory = documentFactory;

            _pendingUpdates = ThreadHelper.JoinableTaskContext.CreateCollection();
            _joinableTaskFactory = ThreadHelper.JoinableTaskContext.CreateFactory(_pendingUpdates);

            _pathBox = new TextBox
            {
                IsReadOnly = true,
                IsReadOnlyCaretVisible = false,
                IsUndoEnabled = false,
                AcceptsReturn = false,
                AcceptsTab = false,
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                Padding = new Thickness(0),
                Margin = new Thickness(6, 0, 6, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.NoWrap,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Cursor = Cursors.IBeam,
                FontSize = 11.0,
                Focusable = true
            };

            // Follow the VS theme, including live theme switches.
            _pathBox.SetResourceReference(Control.ForegroundProperty, EnvironmentColors.ToolWindowTextBrushKey);
            SetResourceReference(BackgroundProperty, EnvironmentColors.ScrollBarBackgroundBrushKey);
            SetResourceReference(BorderBrushProperty, EnvironmentColors.ToolWindowBorderBrushKey);

            BorderThickness = new Thickness(0, 1, 0, 0);
            MinHeight = 20.0;
            ClipToBounds = true;
            Child = _pathBox;

            _pathBox.PreviewKeyDown += OnPathBoxPreviewKeyDown;

            _textView.Closed += OnTextViewClosed;
            _documentFactory.TextDocumentCreated += OnTextDocumentCreated;
            _documentFactory.TextDocumentDisposed += OnTextDocumentDisposed;

            ITextDocument document;
            if (_documentFactory.TryGetTextDocument(DocumentBuffer, out document))
            {
                AttachDocument(document);
            }
            else
            {
                UpdateDisplay();
            }
        }

        private ITextBuffer DocumentBuffer
        {
            get
            {
                // DocumentBuffer is the on-disk buffer even when the view shows a projection
                // (Razor, diff views, embedded languages).
                return _textView.TextDataModel != null
                    ? _textView.TextDataModel.DocumentBuffer
                    : _textView.TextBuffer;
            }
        }

        #region IWpfTextViewMargin

        /// <summary>The WPF element rendered inside the bottom margin container.</summary>
        public FrameworkElement VisualElement
        {
            get { ThrowIfDisposed(); return this; }
        }

        /// <summary>For a horizontal margin this is its height.</summary>
        public double MarginSize
        {
            get { ThrowIfDisposed(); return ActualHeight; }
        }

        public bool Enabled
        {
            get { ThrowIfDisposed(); return true; }
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return string.Equals(marginName, MarginName, StringComparison.OrdinalIgnoreCase) ? this : null;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            _pathBox.PreviewKeyDown -= OnPathBoxPreviewKeyDown;
            _textView.Closed -= OnTextViewClosed;
            _documentFactory.TextDocumentCreated -= OnTextDocumentCreated;
            _documentFactory.TextDocumentDisposed -= OnTextDocumentDisposed;
            DetachDocument();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Document tracking

        private void AttachDocument(ITextDocument document)
        {
            if (ReferenceEquals(_document, document))
            {
                return;
            }

            DetachDocument();

            _document = document;
            if (_document != null)
            {
                _document.FileActionOccurred += OnFileActionOccurred;
            }

            UpdateDisplay();
        }

        private void DetachDocument()
        {
            if (_document != null)
            {
                _document.FileActionOccurred -= OnFileActionOccurred;
                _document = null;
            }
        }

        private void OnTextDocumentCreated(object sender, TextDocumentEventArgs e)
        {
            if (e.TextDocument != null && ReferenceEquals(e.TextDocument.TextBuffer, DocumentBuffer))
            {
                RunOnUiThread(() => AttachDocument(e.TextDocument));
            }
        }

        private void OnTextDocumentDisposed(object sender, TextDocumentEventArgs e)
        {
            if (ReferenceEquals(e.TextDocument, _document))
            {
                RunOnUiThread(() =>
                {
                    DetachDocument();
                    UpdateDisplay();
                });
            }
        }

        private void OnFileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            // Covers Save As and renames, which change ITextDocument.FilePath.
            RunOnUiThread(UpdateDisplay);
        }

        private void OnTextViewClosed(object sender, EventArgs e)
        {
            Dispose();
        }

        #endregion

        private void UpdateDisplay()
        {
            if (_isDisposed)
            {
                return;
            }

            string filePath = _document != null ? _document.FilePath : null;

            if (string.IsNullOrEmpty(filePath))
            {
                _pathBox.Text = NoFileText;
                _pathBox.ToolTip = null;
            }
            else
            {
                _pathBox.Text = filePath;
                _pathBox.ToolTip = filePath;
            }

            // Keep the start of the path visible when the margin is narrower than the text.
            _pathBox.CaretIndex = 0;
            _pathBox.ScrollToHome();
        }

        private void OnPathBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Give Escape back to the editor so focus returns to the code where the user expects it.
            if (e.Key == Key.Escape)
            {
                _textView.VisualElement.Focus();
                e.Handled = true;
            }
        }

        private void RunOnUiThread(Action action)
        {
            if (_isDisposed)
            {
                return;
            }

            if (ThreadHelper.CheckAccess())
            {
                action();
                return;
            }

            _joinableTaskFactory.RunAsync(async delegate
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                if (!_isDisposed)
                {
                    action();
                }
            }).FileAndForget("vs/filepathonfooter/updatemargin");
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(MarginName);
            }
        }
    }
}
