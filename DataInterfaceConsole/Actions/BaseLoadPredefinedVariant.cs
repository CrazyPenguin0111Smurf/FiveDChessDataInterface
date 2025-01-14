﻿using FiveDChessDataInterface.MemoryHelpers;
using FiveDChessDataInterface.Variants;
using System;
using System.Threading;

namespace DataInterfaceConsole.Actions
{
    abstract class BaseLoadPredefinedVariant : BaseAction
    {
        protected abstract bool UseOnlineVariants { get; }

        protected override void Run()
        {
            AssemblyTrap at = null;
            int safeBoardLimit = -1;
            try
            {
                if (!this.di.IsMatchRunning()) // if the match isnt running, then use the trap mode to inject it
                {
                    at = this.di.asmHelper.PlaceAssemblyTrap(IntPtr.Add(this.di.GameProcess.MainModule.BaseAddress, 0x289C2));
                    Console.WriteLine("Main thread trapped. Please start a game, and then check back here.");
                }
                else
                {
                    safeBoardLimit = this.di.MemLocChessArrayCapacity.GetValue();
                    Console.WriteLine($"Be advised that injecting a variant with a size of more than {safeBoardLimit} boards during an ongoing match will possibly lead to a crash.\n" +
                        $"You should consider exiting out to the main menu and rerunning the command.");
                }
                WaitForIngame();

                if (at != null)
                    Thread.Sleep(1000);

                if (this.UseOnlineVariants && !GithubVariantGetter.IsCached)
                    WriteLineIndented("Getting variants from github...");

                var variants = this.UseOnlineVariants ? GithubVariantGetter.GetAllVariants(false, false) : GithubVariantGetter.GetAllVariants(true, true);

                WriteLineIndented("Select a variant from the following:" + (at == null ? " (Variants that would may lead to a crash in case they are loaded right now are indicated in yellow)" : ""));
                for (int i = 0; i < variants.Length; i++)
                {
                    var originalConsoleColor = Console.ForegroundColor;
                    if (at == null) // if we are using direct injection, build the variant and check how many boards it has.
                    {
                        var boardCount = variants[i].GetGameBuilder().Build().Length;
                        if (boardCount > safeBoardLimit)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                        }
                    }

                    WriteLineIndented($"{(i + 1).ToString().PadLeft((int)Math.Ceiling(Math.Log10(variants.Length)))}. {variants[i].Name} by {variants[i].Author}");

                    Console.ForegroundColor = originalConsoleColor;
                }

                if (int.TryParse(Util.ConsoleReadLineWhile(() => this.di.IsValid()), out int input) && input > 0 && input <= variants.Length)
                {
                    var chosenVariant = variants[input - 1];
                    WriteLineIndented($"Loading variant '{chosenVariant.Name}'...");
                    var gb = chosenVariant.GetGameBuilder();
                    this.di.SetChessBoardArrayFromBuilder(gb);
                    WriteLineIndented($"Variant loaded and written to memory.");
                }
                else
                {
                    WriteLineIndented("Invalid input. Not loading any variant.");
                }
            }
            finally
            {
                if (at != null)
                {
                    WriteLineIndented("Untrapped game thread.");
                    at.ReleaseTrap();
                }
            }
        }
    }
}
