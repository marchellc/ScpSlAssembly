using System;
using System.Xml;

namespace LiteNetLib4Mirror.Open.Nat;

internal abstract class ResponseMessageBase
{
	private readonly XmlDocument _document;

	protected string ServiceType;

	private readonly string _typeName;

	protected ResponseMessageBase(XmlDocument response, string serviceType, string typeName)
	{
		this._document = response;
		this.ServiceType = serviceType;
		this._typeName = typeName;
	}

	protected XmlNode GetNode()
	{
		XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(this._document.NameTable);
		xmlNamespaceManager.AddNamespace("responseNs", this.ServiceType);
		string typeName = this._typeName;
		string text = typeName.Substring(0, typeName.Length - "Message".Length);
		return this._document.SelectSingleNode("//responseNs:" + text, xmlNamespaceManager) ?? throw new InvalidOperationException("The response is invalid: " + text);
	}
}
