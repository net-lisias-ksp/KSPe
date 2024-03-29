﻿/*
	This file is part of KSPe, a component for KSP Enhanced /L
		© 2018-2024 LisiasT : http://lisias.net <support@lisias.net>

	KSP Enhanced /L is double licensed, as follows:
		* SKL 1.0 : https://ksp.lisias.net/SKL-1_0.txt
		* GPL 2.0 : https://www.gnu.org/licenses/gpl-2.0.txt

	And you are allowed to choose the License that better suit your needs.

	KSP Enhanced /L is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

	You should have received a copy of the SKL Standard License 1.0
	along with KSP Enhanced /L. If not, see <https://ksp.lisias.net/SKL-1_0.txt>.

	You should have received a copy of the GNU General Public License 2.0
	along with KSP Enhanced /L. If not, see <https://www.gnu.org/licenses/>.

*/
using System;

namespace KSPe
{
	internal static class SanityChecks
	{
		internal static void DoIt()
		{
			CheckForPwd();
			CheckForApplicationRootPath();
		}

		private static void CheckForPwd()
		{
			string pwd = KSPe.IO.Directory.GetCurrentDirectory();
			string origin = KSPe.IO.Path.Origin();

			Log.detail("System.IO.Directory.GetCurrentDirectory: {0}", System.IO.Directory.GetCurrentDirectory());
			Log.detail("KSPe.IO.Directory.GetCurrentDirectory:   {0}", pwd);
			Log.detail("KSPe.IO.Path.Origin:                     {0}", origin);

			// KSPe will not run on this scenario, even if theoretically possible - KSP itself doesn't behaves, so there's no point
			// (and it would be dangerous) on proceeding. There're suicidal users out there willing to try this, but I'm not going
			// to put myself on a situation in which I could prevent a disaster and did nothing about.

			// Naivelly comparing the paths is borking on Windows, as this thingy uses case insensity pathnames by default.
			// if (!pwd.Equals(origin)) FatalErrors.PwdIsNotOrigin.Show(pwd, origin);

			// So we need a mechanism that would solve this on Windows without breaking Linux and MacOS, ideally without creating
			// system specific support code. The solution for this is using URIs.
			// Let the runtime do the dirty work for us.
			{
				Uri uri_pwd = new Uri(System.IO.Path.Combine(pwd, "dummy.txt"));
				Uri uri_origin = new Uri(System.IO.Path.Combine(origin, "dummy.txt"));

				if (uri_pwd != uri_origin) FatalErrors.PwdIsNotOrigin.Show(pwd, origin);
			}
		}

		private static void CheckForApplicationRootPath()
		{
			string origin = KSPe.IO.Path.Origin();
			string app_root = KSPe.IO.Path.AppRoot();

			Log.detail("KSPUtil.ApplicationRootPath {0}", KSPUtil.ApplicationRootPath);
			Log.detail("KSPe.IO.Path.AppRoot        {0}", app_root);
			Log.detail("KSPe.IO.Path.Origin:        {0}", origin);

			// On KSP 1.12.4, PD made a bad move on the PD Launcher that ended up users trying to get rid of it anyway they could.
			// This ended up with a screwed KSP rig, as the pwd is now on the PD-Launcher directory, and not on KSP's anymore.
			// See https://forum.kerbalspaceprogram.com/index.php?/topic/210419-get-rid-of-the-stupid-launcher-nobody-likes-them-and-they-do-nothing-but-ruin-the-gaming-experience/&do=findComment&comment=4196378
			// for the gory details.
			{
				Uri uri_origin = new Uri(System.IO.Path.Combine(origin, "dummy.txt"));
				Uri uri_approot = new Uri(System.IO.Path.Combine(app_root, "dummy.txt"));

				if (!uri_approot.Equals(uri_origin)) FatalErrors.ApplicationRootPathIsNotOrigin.Show(KSPUtil.ApplicationRootPath, origin);
			}
		}
	}
}
