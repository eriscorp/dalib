using System.CommandLine;
using Foscail.Commands;

var root = new RootCommand("Foscail — command-line tools for Dark Ages (DOOMVAS v1) client data.");
root.Add(UnpackCommand.Build());

return root.Parse(args).Invoke();
