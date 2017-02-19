﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.IO.Compression;
using System.Drawing;
using System.Drawing.Imaging;
using System.Xml.XPath;
using System.Xml;
using System.Threading;
using ManicDigger.Common;

namespace ManicDigger.ClientNative
{
	[XmlRoot("dictionary")]
	public class SerializableDictionary<TKey, TValue>
        : Dictionary<TKey, TValue>, IXmlSerializable
	{
		#region IXmlSerializable Members
		public System.Xml.Schema.XmlSchema GetSchema()
		{
			return null;
		}
		public void ReadXml(System.Xml.XmlReader reader)
		{
			XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
			XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));

			bool wasEmpty = reader.IsEmptyElement;
			reader.Read();

			if (wasEmpty) {
				return;
			}

			while (reader.NodeType != System.Xml.XmlNodeType.EndElement) {
				reader.ReadStartElement("item");

				reader.ReadStartElement("key");
				TKey key = (TKey)keySerializer.Deserialize(reader);
				reader.ReadEndElement();

				reader.ReadStartElement("value");
				TValue value = (TValue)valueSerializer.Deserialize(reader);
				reader.ReadEndElement();

				this.Add(key, value);

				reader.ReadEndElement();

				reader.MoveToContent();
			}
			reader.ReadEndElement();
		}

		public void WriteXml(System.Xml.XmlWriter writer)
		{
			XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
			XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));

			foreach (TKey key in this.Keys) {
				writer.WriteStartElement("item");

				writer.WriteStartElement("key");
				keySerializer.Serialize(writer, key);
				writer.WriteEndElement();

				writer.WriteStartElement("value");
				TValue value = this[key];
				valueSerializer.Serialize(writer, value);

				writer.WriteEndElement();

				writer.WriteEndElement();
			}
		}
		#endregion
	}
	
	
	struct TextAndSize
	{
		public string text;
		public float size;
		public override int GetHashCode()
		{
			if (text == null) {
				return 0;
			}
			return text.GetHashCode() ^ size.GetHashCode();
		}
		public override bool Equals(object obj)
		{
			if (obj is TextAndSize) {
				TextAndSize other = (TextAndSize)obj;
				return this.text == other.text && this.size == other.size;
			}
			return base.Equals(obj);
		}
	}
	//Doesn't work on Ubuntu - pointer access crashes.
	public class FastBitmap
	{
		public Bitmap bmp { get; set; }
		BitmapData bmd;
		public void Lock()
		{
			if (bmd != null) {
				throw new Exception("Already locked.");
			}
			if (bmp.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb) {
				bmp = new Bitmap(bmp);
			}
			bmd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
				System.Drawing.Imaging.ImageLockMode.ReadOnly, bmp.PixelFormat);
		}
		public int GetPixel(int x, int y)
		{
			if (bmd == null) {
				throw new Exception();
			}
			unsafe {
				int* row = (int*)((byte*)bmd.Scan0 + (y * bmd.Stride));
				return row[x];
			}
		}
		public void SetPixel(int x, int y, int color)
		{
			if (bmd == null) {
				throw new Exception();
			}
			unsafe {
				int* row = (int*)((byte*)bmd.Scan0 + (y * bmd.Stride));
				row[x] = color;
			}
		}
		public void Unlock()
		{
			if (bmd == null) {
				throw new Exception("Not locked.");
			}
			bmp.UnlockBits(bmd);
			bmd = null;
		}
	}
}
