Attribute VB_Name = "SMRI_Exporter_Launcher"
Option Explicit

Public Sub SMRI_RunExportMaker()
    Dim shell As Object
    Set shell = CreateObject("WScript.Shell")
    shell.Run """C:\SMRI\SMRIExporter\SMRI.Exporter.exe""", 1, False
End Sub