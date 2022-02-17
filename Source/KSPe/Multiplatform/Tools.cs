﻿/*
	This file is part of KSPe, a component for KSP API Extensions/L
		© 2018-21 Lisias T : http://lisias.net <support@lisias.net>

	KSPe API Extensions/L is double licensed, as follows:
		* SKL 1.0 : https://ksp.lisias.net/SKL-1_0.txt
		* GPL 2.0 : https://www.gnu.org/licenses/gpl-2.0.txt

	And you are allowed to choose the License that better suit your needs.

	KSPe API Extensions/L is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

	You should have received a copy of the SKL Standard License 1.0
	along with KSPe API Extensions/L. If not, see <https://ksp.lisias.net/SKL-1_0.txt>.

	You should have received a copy of the GNU General Public License 2.0
	along with KSPe API Extensions/L. If not, see <https://www.gnu.org/licenses/>.

*/
using System;
namespace KSPe.Multiplatform
{
	internal static class Tools
	{
		internal static object CreateInstanceByInterface(string ifcName)
		{
			Log.debug("Looking for {0}", ifcName);
			foreach(System.Reflection.Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
				foreach(System.Type type in assembly.GetTypes())
					foreach(System.Type ifc in type.GetInterfaces() )
					{
						Log.debug("Checking {0} {1} {2}", assembly, type, ifc);
						if (ifcName == ifc.ToString())
						{
						Log.debug("Found one! {0}", ifc);
							if (type.FullName.StartsWith("KSPe.")) // Failsafe. We should not mess with anything not made for KSPe.
							{
								object r = System.Activator.CreateInstance(type);
								Log.debug("Type of result {0}", r.GetType());
								return r;
							}
						}
					}
			return null;
		}
		private static readonly KSPe.Util.Log.Logger Log = KSPe.Util.Log.Logger.CreateForType<Startup>("KSPe.Multiplatform", "Tools", 0);
	}
}