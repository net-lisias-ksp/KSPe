﻿/*
	This file is part of KSPe.UI.14, a component for KSP Enhanced /L
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
using UnityEngine;

namespace KSPe.KSP14.UI
{
	public class GUI : KSPe.UI.GUI.Interface
	{
		public Rect Window (int id, Rect clientRect, UnityEngine.GUI.WindowFunction func, string text)						{ return ClickThroughFix.ClickThruBlocker.GUIWindow(id, clientRect, func, text); }
		public Rect Window (int id, Rect clientRect, UnityEngine.GUI.WindowFunction func, Texture image)					{ return ClickThroughFix.ClickThruBlocker.GUIWindow(id, clientRect, func, image); }
		public Rect Window (int id, Rect clientRect, UnityEngine.GUI.WindowFunction func, GUIContent content)				{ return ClickThroughFix.ClickThruBlocker.GUIWindow(id, clientRect, func, content); }
		public Rect Window (int id, Rect clientRect, UnityEngine.GUI.WindowFunction func, string text, GUIStyle style)		{ return ClickThroughFix.ClickThruBlocker.GUIWindow(id, clientRect, func, text, style); }
		public Rect Window (int id, Rect clientRect, UnityEngine.GUI.WindowFunction func, Texture image, GUIStyle style)	{ return ClickThroughFix.ClickThruBlocker.GUIWindow(id, clientRect, func, image, style); }
		public Rect Window (int id, Rect clientRect, UnityEngine.GUI.WindowFunction func, GUIContent title, GUIStyle style)	{ return ClickThroughFix.ClickThruBlocker.GUIWindow(id, clientRect, func, title, style); }

		// FIXME : https://github.com/linuxgurugamer/ClickThroughBlocker/issues/6
		public Rect ModalWindow (int id, Rect clientRect, UnityEngine.GUI.WindowFunction func, string text)							{ return UnityEngine.GUI.ModalWindow(id, clientRect, func, text); } 
        public Rect ModalWindow (int id, Rect clientRect, UnityEngine.GUI.WindowFunction func, Texture image)						{ return UnityEngine.GUI.ModalWindow(id, clientRect, func, image); }
		public Rect ModalWindow (int id, Rect clientRect, UnityEngine.GUI.WindowFunction func, GUIContent content)					{ return UnityEngine.GUI.ModalWindow(id, clientRect, func, content); }
        public Rect ModalWindow (int id, Rect clientRect, UnityEngine.GUI.WindowFunction func, string text, GUIStyle style)			{ return UnityEngine.GUI.ModalWindow(id, clientRect, func, text, style); }
        public Rect ModalWindow (int id, Rect clientRect, UnityEngine.GUI.WindowFunction func, Texture image, GUIStyle style)		{ return UnityEngine.GUI.ModalWindow(id, clientRect, func, image, style); }
        public Rect ModalWindow (int id, Rect clientRect, UnityEngine.GUI.WindowFunction func, GUIContent content, GUIStyle style)	{ return UnityEngine.GUI.ModalWindow(id, clientRect, func, content, style); }
	}
}
