using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LiteNetLib4Mirror.Open.Nat
{
	internal class SoapClient
	{
		public SoapClient(Uri url, string serviceType)
		{
			this._url = url;
			this._serviceType = serviceType;
		}

		public async Task<XmlDocument> InvokeAsync(string operationName, IDictionary<string, object> args)
		{
			byte[] messageBody = this.BuildMessageBody(operationName, args);
			HttpWebRequest request = this.BuildHttpWebRequest(operationName, messageBody);
			if (messageBody.Length != 0)
			{
				Stream stream2 = await request.GetRequestStreamAsync();
				using (Stream stream = stream2)
				{
					await stream.WriteAsync(messageBody, 0, messageBody.Length);
				}
				Stream stream = null;
			}
			XmlDocument xmlDocument2;
			using (WebResponse webResponse = await SoapClient.GetWebResponse(request))
			{
				Stream responseStream = webResponse.GetResponseStream();
				long contentLength = webResponse.ContentLength;
				StreamReader streamReader = new StreamReader(responseStream, Encoding.UTF8);
				XmlDocument xmlDocument = this.GetXmlDocument((contentLength != -1L) ? streamReader.ReadAsMany((int)contentLength) : streamReader.ReadToEnd());
				webResponse.Close();
				xmlDocument2 = xmlDocument;
			}
			return xmlDocument2;
		}

		private static async Task<WebResponse> GetWebResponse(WebRequest request)
		{
			WebResponse webResponse;
			try
			{
				webResponse = await request.GetResponseAsync();
			}
			catch (WebException ex)
			{
				webResponse = ex.Response as HttpWebResponse;
				if (webResponse == null)
				{
					throw;
				}
			}
			return webResponse;
		}

		private HttpWebRequest BuildHttpWebRequest(string operationName, byte[] messageBody)
		{
			HttpWebRequest httpWebRequest = WebRequest.CreateHttp(this._url);
			httpWebRequest.KeepAlive = false;
			httpWebRequest.Method = "POST";
			httpWebRequest.ContentType = "text/xml; charset=\"utf-8\"";
			httpWebRequest.Headers.Add("SOAPACTION", string.Concat(new string[] { "\"", this._serviceType, "#", operationName, "\"" }));
			httpWebRequest.ContentLength = (long)messageBody.Length;
			return httpWebRequest;
		}

		private byte[] BuildMessageBody(string operationName, IEnumerable<KeyValuePair<string, object>> args)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("<s:Envelope ");
			stringBuilder.AppendLine("   xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" ");
			stringBuilder.AppendLine("   s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">");
			stringBuilder.AppendLine("   <s:Body>");
			stringBuilder.AppendLine(string.Concat(new string[] { "\t  <u:", operationName, " xmlns:u=\"", this._serviceType, "\">" }));
			foreach (KeyValuePair<string, object> keyValuePair in args)
			{
				stringBuilder.AppendLine(string.Concat(new string[]
				{
					"\t\t <",
					keyValuePair.Key,
					">",
					Convert.ToString(keyValuePair.Value, CultureInfo.InvariantCulture),
					"</",
					keyValuePair.Key,
					">"
				}));
			}
			stringBuilder.AppendLine("\t  </u:" + operationName + ">");
			stringBuilder.AppendLine("   </s:Body>");
			stringBuilder.Append("</s:Envelope>\r\n\r\n");
			string text = stringBuilder.ToString();
			return Encoding.UTF8.GetBytes(text);
		}

		private XmlDocument GetXmlDocument(string response)
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(response);
			XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
			xmlNamespaceManager.AddNamespace("errorNs", "urn:schemas-upnp-org:control-1-0");
			XmlNode xmlNode;
			if ((xmlNode = xmlDocument.SelectSingleNode("//errorNs:UPnPError", xmlNamespaceManager)) != null)
			{
				int num = Convert.ToInt32(xmlNode.GetXmlElementText("errorCode"), CultureInfo.InvariantCulture);
				string xmlElementText = xmlNode.GetXmlElementText("errorDescription");
				NatDiscoverer.TraceSource.LogWarn("Server failed with error: {0} - {1}", new object[] { num, xmlElementText });
				throw new MappingException(num, xmlElementText);
			}
			return xmlDocument;
		}

		private readonly string _serviceType;

		private readonly Uri _url;
	}
}
