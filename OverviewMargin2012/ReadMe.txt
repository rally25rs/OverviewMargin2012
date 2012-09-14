AllMargins

Introduction:
    This is a compilation of the OverviewMargin and several other extensions that use the OverviewMargin. The OverviewMargin shows
    a margin on the right side of the editor that logically maps to the entire file (similar the the vertical scroll bar). Unlike
    the scroll bar, it maps to the entire file and can contain other margins that provide more information about the file.

History:
    v1.0    David Pugh  2/26/2010
        Initial release
    V1.1    David Pugh  3/4/2010
        Updated included extensions.
    V1.2    David Pugh  4/19/2010
        Updated included extensions.
    V1.3    David Pugh  4/20/2010
        Updated included extensions.
    V1.4    David Pugh  4/22/2010
        Updated included extensions.
    V1.5    David Pugh  4/27/2010
        Updated included extensions.
    V1.6    David Pugh  4/28/2010
        Updated included extensions to fix VB parser error.
    V1.7    David Pugh  4/28/2010
        Updated version number to work around install issue.
    V1.8    David Pugh  4/28/2010
        More VB parse fixes (Public, REM, Interface, Structure).
    V1.9    David Pugh  4/28/2010
        Updated CaretMargin, VB, C# and C parsers.
    V2.0    David Pugh  5/03/2010
        Bumped version number to fix installation problem.
    V2.1    David Pugh  6/10/2010
        Updated OverviewMargin, BlockTagger, StructureAdornment & StructureMargin.
        Changed namespaces and DLL names to Microsoft.VisualStudio.Extensions.....
    V2.2    David Pugh  6/29/2010
        Updated StructureAdornment.
    V2.3    David Pugh  6/30/2010
        Picked up fix for VB parser, tweaks for structure adornment & margin.
    V3.0    Jeff Valore 9/06/2012
        Updated for VisualStudio 2012. All extensions now included in single project.

Included extensions:
        CaretMargin         http://visualstudiogallery.msdn.microsoft.com/en-us/a893687b-f488-49eb-ad91-c59d86daad34.
        ErrorsToMarks       http://visualstudiogallery.msdn.microsoft.com/en-us/0fc52c83-0ab3-485d-a917-2006966eec7a.
        MarkersToMarks      http://visualstudiogallery.msdn.microsoft.com/en-us/89deee06-0ed0-4347-81a6-942a3f2874af.
        OverviewMarginImpl  http://visualstudiogallery.msdn.microsoft.com/en-us/2e9f37b7-5a1f-4c47-930b-379b2d0fd596.
        StructureAdornment  http://visualstudiogallery.msdn.microsoft.com/en-us/203f22f4-3e9f-4dbb-befc-f2606835834e.
        StructureMargin     http://visualstudiogallery.msdn.microsoft.com/en-us/fe432eb5-c538-47a9-9919-fba1a8f5b261. 

        BlockTagger         The definition of an API to get the structure of a code file.
        BlockTaggerImpl     An implementation of the BlockTaggerAPI for C/C# and VB files.

        OverviewMargin      The definition of an API for creating margins that map to the entire file.

        SettingsStore       The definition of an API to load and save editor options across sessions.
        SettingsStoreImpl   An implementation of the SettingStore API that uses IVsSettingsStore to access the system registry.

Usage:
    See other extensions.

Options:
    None.

Notes:
    The full source for this extension is at http://code.msdn.microsoft.com/OverviewMargin/Release/ProjectReleases.aspx?ReleaseId=3957