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
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace FilePathOnFooter
{
    /// <summary>
    /// Package registration for the extension.
    /// <para>
    /// The margin itself is a pure MEF component and works without this package ever loading, so
    /// the package deliberately declares no auto-load context: it exists to register the extension
    /// (Help -> About, Extension Manager) and as the host for any future commands or options.
    /// </para>
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(
        productName: "FilePathOnFooter",
        productDetails: "Shows the current document's full file path in the editor's bottom margin.",
        productId: "1.0")]
    [Guid(PackageGuidString)]
    public sealed class FilePathOnFooterPackage : AsyncPackage
    {
        /// <summary>Package GUID, kept in sync with the pkgdef generated at build time.</summary>
        public const string PackageGuidString = "7627ef60-78ce-47f6-9c24-80bdee828d7d";

        /// <summary>
        /// Async initialization. Runs on a background thread; switch to the UI thread with
        /// <c>await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken)</c> before
        /// touching any UI or non-free-threaded VS service.
        /// </summary>
        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
        }
    }
}
