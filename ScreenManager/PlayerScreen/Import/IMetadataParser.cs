/*
Copyright � Joan Charmant 2008.
joan.charmant@gmail.com 
 
This file is part of Kinovea.

Kinovea is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License version 2 
as published by the Free Software Foundation.

Kinovea is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Kinovea. If not, see http://www.gnu.org/licenses/.

*/
using System;
using System.Xml;

namespace Kinovea.ScreenManager
{
	/// <summary>
	/// Interface for all metadata parsers.
	/// </summary>
	public interface IMetadataParser
	{
		void Parse(XmlTextReader _xmlReader, Metadata _metadata);
	}
}
