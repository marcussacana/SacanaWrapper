# SacanaWrapper

A Wrapper to use my tools as plugin to your software.

  - Included a txt export/import as default
  - Auto Plugin Detection (is little brute)
  - Massive Directory/Files Import/Export with only a drag&drop

# How works?

  - Search for .ini files into a directory called "plugins"
  - The .ini contains information of the Namespace, Class and In/Out void.
  - The wrapper try found a .cs (c# source) or .dll; and try run it...
  - Using reflection, he try call the function specified by the .ini into the .cs or .dll


# Default/Sample Plugins:
  - KiriKiri [.SCN] [PSB/MDF]
  - SiglusEngine [.SS] [NONE]
  - ARCGameEngine [.BIN] [SYS]
  - AutomataTranslator [.BIN/.TMD/.SMD] [RITE/NONE/NONE]
  - Renpy [.RPY] (Decompiled Script filter)