'<RequiredAssemblies>System.Web.dll;System.Core.dll;System.Xml.dll;System.Xml.Linq.dll;System.Configuration.dll;</RequiredAssemblies>
Imports System.Linq
Public Class CustomBundle
    Private Const BuildName As String = "CustomBundle"
    Public mVirtualDirectoryPath As String

    Public Function ExecutePackage() As String
        Try
            UpdateFiles()
            UpdateWebConfig()
        Catch ex As System.Exception
            Return ex.Message
        End Try
        Return "SUCCESS"
    End Function

#Region " UPDATE FILES "
    Private Sub UpdateFiles()
        DirectoryCopy(String.Format("{0}/Packages/{1}/ReleaseFiles", mVirtualDirectoryPath, BuildName), mVirtualDirectoryPath)
    End Sub

    Private Shared Sub DirectoryCopy(ByVal sourceDirName As String, ByVal destDirName As String)
        ' Get the subdirectories for the specified directory. 
        Dim dir As System.IO.DirectoryInfo = New System.IO.DirectoryInfo(sourceDirName)
        Dim dirs As System.IO.DirectoryInfo() = dir.GetDirectories()

        If Not dir.Exists Then
            Throw New System.IO.DirectoryNotFoundException(
                "Source directory does not exist or could not be found: " _
                + sourceDirName)
        End If

        ' If the destination directory doesn't exist, create it. 
        If Not System.IO.Directory.Exists(destDirName) Then
            System.IO.Directory.CreateDirectory(destDirName)
        End If

        ' Get the files in the directory and copy them to the new location. 
        Dim files As System.IO.FileInfo() = dir.GetFiles()
        For Each file As System.IO.FileInfo In files
            Dim temppath As String = System.IO.Path.Combine(destDirName, file.Name)
            file.CopyTo(temppath, True)
        Next file

        ' If copying subdirectories, copy them and their contents to new location. 
        For Each subdir As System.IO.DirectoryInfo In dirs
            Dim temppath As String = System.IO.Path.Combine(destDirName, subdir.Name)
            DirectoryCopy(subdir.FullName, temppath)
        Next subdir
    End Sub
#End Region

#Region " UPDATE WEB CONFIG "
    Private Sub UpdateWebConfig()
        Dim configuration As System.Configuration.Configuration = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("~")
        Dim appSettingsSection As System.Configuration.AppSettingsSection = configuration.GetSection("appSettings")
        If appSettingsSection.Settings("CurrentBuild") Is Nothing = False Then
            appSettingsSection.Settings("CurrentBuild").Value = 
        End If
        configuration.Save()
    End Sub

#End Region

#Region " GET SQL SNAPSHOT "
    Public Function GetSQLSnapshot() As String
        If System.IO.File.Exists(String.Format("{0}/Packages/{1}/{1}.snp", mVirtualDirectoryPath, BuildName)) Then
            Return String.Format("{0}/Packages/{1}/{1}.snp", mVirtualDirectoryPath, BuildName)
        End If
        Return ""
    End Function
#End Region


#Region " GET SQL SCRIPTS "
    Public Function GetSQLScripts() As String
        Return <![CDATA[
            
             ]]>.Value
    End Function
#End Region

End Class