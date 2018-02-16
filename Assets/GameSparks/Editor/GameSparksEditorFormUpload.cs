using System;
using System.Threading;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.IO;

namespace GameSparks.Editor
{
    /// <summary>
    /// Helper class for form uploads to the GameSparks Rest API. 
    /// </summary>
    public static class GameSparksEditorFormUpload
    {
    	private static readonly Encoding encoding = Encoding.UTF8;

    	public static string UploadFile(string url, string fileName, string username, string password){

    		FileParameter param = new FileParameter(GetBytesFromFile(fileName), Path.GetFileName(fileName));
    		param.FileName = fileName;

    		IDictionary<string, object> postParams = new Dictionary<string, object>();
    		postParams.Add("binaryContentFile", param);

    		return MultipartFormDataPost(url, postParams, username, password); 
    	}

    	public static byte[] GetBytesFromFile(string fullFilePath)
    	{
    		FileStream fs = null;
    		try
    		{
    			fs = File.OpenRead(fullFilePath);
    			byte[] bytes = new byte[fs.Length];
    			fs.Read(bytes, 0, Convert.ToInt32(fs.Length));
    			return bytes;
    		}
    		finally
    		{
    			if (fs != null)
    			{
    				fs.Close();
    				fs.Dispose();
    			}
    		}
    		
    	}
    	
    	public static string MultipartFormDataPost(string postUrl, IDictionary<string, object> postParameters, string userName, String password)
    	{
    		string formDataBoundary = String.Format("----------{0:N}", Guid.NewGuid());
    		string contentType = "multipart/form-data; boundary=" + formDataBoundary;
    		
    		byte[] formData = GetMultipartFormData(postParameters, formDataBoundary);
    		
    		return PostForm(postUrl, contentType, formData, userName, password);

    	}

    	private static String PostForm(string postUrl, string contentType, byte[] formData, string username, string password)
    	{
    		HttpWebRequest request = WebRequest.Create(postUrl) as HttpWebRequest;
    		
    		if (request == null)
    		{
    			throw new NullReferenceException("request is not a http request");
    		}
    		
    		// Set up the request properties.
    		request.Method = "POST";
    		request.ContentType = contentType;
    		request.UserAgent = "Unity Editor";
    		request.CookieContainer = new CookieContainer();
    		request.ContentLength = formData.Length;
    		
    		// You could add authentication here as well if needed:
    		request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(username + ":" + password)));

    		Stream requestStream = request.GetRequestStream();
    		requestStream.Write(formData, 0, formData.Length);
    		requestStream.Close();

    		WebResponse webResponse =  request.GetResponse();
    		StreamReader responseReader = new StreamReader(webResponse.GetResponseStream());
    		return responseReader.ReadToEnd();
    	}
    	

    	private static byte[] GetMultipartFormData(IDictionary<string, object> postParameters, string boundary)
    	{
    		Stream formDataStream = new System.IO.MemoryStream();
    		bool needsCLRF = false;
    		
    		foreach (var param in postParameters)
    		{
    			// Thanks to feedback from commenters, add a CRLF to allow multiple parameters to be added.
    			// Skip it on the first parameter, add it to subsequent parameters.
    			if (needsCLRF)
    				formDataStream.Write(encoding.GetBytes("\r\n"), 0, encoding.GetByteCount("\r\n"));
    			
    			needsCLRF = true;
    			
    			if (param.Value is FileParameter)
    			{
    				FileParameter fileToUpload = (FileParameter)param.Value;
    				
    				// Add just the first part of this param, since we will write the file data directly to the Stream
    				string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\";\r\nContent-Type: {3}\r\n\r\n",
    				                              boundary,
    				                              param.Key,
    				                              fileToUpload.FileName ?? param.Key,
    				                              fileToUpload.ContentType ?? "application/octet-stream");
    				
    				formDataStream.Write(encoding.GetBytes(header), 0, encoding.GetByteCount(header));
    				
    				// Write the file data directly to the Stream, rather than serializing it to a string.
    				formDataStream.Write(fileToUpload.File, 0, fileToUpload.File.Length);
    			}
    			else
    			{
    				string postData = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}",
    				                                boundary,
    				                                param.Key,
    				                                param.Value);
    				formDataStream.Write(encoding.GetBytes(postData), 0, encoding.GetByteCount(postData));
    			}
    		}
    		
    		// Add the end of the request.  Start with a newline
    		string footer = "\r\n--" + boundary + "--\r\n";
    		formDataStream.Write(encoding.GetBytes(footer), 0, encoding.GetByteCount(footer));
    		
    		// Dump the Stream into a byte[]
    		formDataStream.Position = 0;
    		byte[] formData = new byte[formDataStream.Length];
    		formDataStream.Read(formData, 0, formData.Length);
    		
    		#if !UNITY_METRO || UNITY_EDITOR
    		formDataStream.Close();
    		#endif
    		
    		return formData;
    	}
    	
    	public class FileParameter
    	{
    		public byte[] File { get; set; }
    		public string FileName { get; set; }
    		public string ContentType { get; set; }
    		public FileParameter(byte[] file) : this(file, null) { }
    		public FileParameter(byte[] file, string filename) : this(file, filename, null) { }
    		public FileParameter(byte[] file, string filename, string contenttype)
    		{
    			File = file;
    			FileName = filename;
    			ContentType = contenttype;
    		}
    	}
    }
}

// namespace documentation

/// <summary>
/// Helper classes for integration of GameSparks SDK into Unity Editor. 
/// </summary>
namespace GameSparks.Editor
{
}