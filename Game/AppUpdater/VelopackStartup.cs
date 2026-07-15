// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;
using Velopack;

namespace Chromonia.AppUpdater;

internal static class VelopackStartup
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255")]
    [ModuleInitializer]
    internal static void Initialize()
    {
        VelopackApp.Build().Run();
    }
}
