Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports RestSharp

Public Class Form1

    Dim StrExtensions = New List(Of String) From {".JPG", ".JPEG", ".PNG", ".PDF"}  '// supported file formats
    Dim apiKey As String = "AIzaSyDccVzo5iwFYIB2sOE98dVDydypA43HdqY"  '// gemini ai api key

    Function GetContentType(filePath As String) As String
        Dim finfo As New IO.FileInfo(filePath)
        Dim strcnttype = ""
        If finfo.Extension.ToUpper = ".PDF" Then
            strcnttype = "application/pdf"
        Else
            strcnttype = "image/" & finfo.Extension.Replace(".", "").ToLower
        End If
        Return strcnttype
    End Function

    Function UploadFile(apiKey As String, filePath As String, ContentType As String) As String

        Dim finfo As New IO.FileInfo(filePath)
        Dim StrUploadurl As String = ""

        '// create a uplpad link
        Dim client As New RestClient($"https://generativelanguage.googleapis.com")  '// upload file endpoint
        Dim request As New RestRequest($"/upload/v1beta/files?key={apiKey}", Method.Post)
        request.AddHeader("X-Goog-Upload-Protocol", "resumable")
        request.AddHeader("X-Goog-Upload-Command", "start")
        request.AddHeader("X-Goog-Upload-Header-Content-Length", finfo.Length.ToString)
        request.AddHeader("X-Goog-Upload-Header-Content-Type", ContentType)
        request.AddHeader("Content-Type", "application/json")
        'request.AddParameter("application/json", "{    \""file\"": {        \""display_name\"": \""My Document\""    } }", ParameterType.RequestBody)
        request.AddBody(New With {.file = New With {.display_name = "Invoice"}}, RestSharp.ContentType.Json)
        Dim response As RestResponse = client.Execute(request, Method.Post)
        If response.StatusCode = Net.HttpStatusCode.OK And response.ErrorException Is Nothing Then
            StrUploadurl = If(response.GetHeaderValue("X-Goog-Upload-URL"), "")
        Else
            Return String.Empty
        End If

        '// upload file 
        client = New RestClient()
        request = New RestRequest(StrUploadurl, Method.Post)
        request.AddHeader("Content-Length", finfo.Length.ToString)
        request.AddHeader("X-Goog-Upload-Offset", "0")
        request.AddHeader("X-Goog-Upload-Command", "upload, finalize")
        request.AddParameter("application/pdf", IO.File.ReadAllBytes(finfo.FullName), ParameterType.RequestBody)
        Dim response2 As RestResponse = client.Execute(request, Method.Post)

        If response2.StatusCode = Net.HttpStatusCode.OK And response2.ErrorException Is Nothing Then
            Dim json As JObject = JObject.Parse(response2.Content)
            If json.ContainsKey("file") Then
                Dim json2 As JObject = json("file")
                If json2.ContainsKey("uri") Then
                    Return json2("uri")
                End If
            End If

            Return String.Empty

        Else
            Return String.Empty
        End If

    End Function

    Function ExtractInvoiceData(apiKey As String, fileUrl As String, ContentType As String) As String

        Dim strrequest = IO.File.ReadAllText(Application.StartupPath & "\invreq.json")
        strrequest = strrequest.Replace("{{doctype}}", ContentType).Replace("{{fileuri}}", fileUrl)


        Dim client As New RestClient($"https://generativelanguage.googleapis.com")
        Dim request As New RestRequest($"/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}", Method.Post)
        request.AddHeader("Content-Type", "application/json")
        request.AddJsonBody(strrequest)
        Dim response As RestResponse = client.Execute(request, Method.Post)
        Return response.Content

    End Function

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        RichTextBox1.Text = ""
        TextBox1.Text = ""
        Dim Dlg As New OpenFileDialog
        If Dlg.ShowDialog = DialogResult.OK Then
            TextBox1.Text = Dlg.FileName
            Dim finfo = New IO.FileInfo(TextBox1.Text)
            If Not StrExtensions.Contains(finfo.Extension.ToUpper) Then
                MsgBox("Invalid File format. Supported formats are .jpg, .jpeg, .png, .pdf")
                TextBox1.Text = ""
            End If
        End If

    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

        RichTextBox1.Text = ""

        If String.IsNullOrWhiteSpace(TextBox1.Text) = False Then
            Dim finfo = New IO.FileInfo(TextBox1.Text)
            Dim StrContentType = GetContentType(TextBox1.Text)

            Dim StrfileUrl = UploadFile(apiKey, TextBox1.Text, StrContentType)

            If Not String.IsNullOrWhiteSpace(StrfileUrl) Then
                Dim Response = ExtractInvoiceData(apiKey, StrfileUrl, StrContentType)
                RichTextBox1.Text = Response
            End If

        End If

    End Sub




End Class
