﻿// /*
// 	This file is part of KSPe, a component for KSP API Extensions/L
// 	(C) 2018-20 Lisias T : http://lisias.net <support@lisias.net>
//
// 	KSPe API Extensions/L is double licensed, as follows:
//
// 	* SKL 1.0 : https://ksp.lisias.net/SKL-1_0.txt
// 	* GPL 2.0 : https://www.gnu.org/licenses/gpl-2.0.txt
//
// 	And you are allowed to choose the License that better suit your needs.
//
// 	KSPe API Extensions/L is distributed in the hope that it will be useful,
// 	but WITHOUT ANY WARRANTY; without even the implied warranty of
// 	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
//
// 	You should have received a copy of the SKL Standard License 1.0
// 	along with KSPe API Extensions/L. If not, see <https://ksp.lisias.net/SKL-1_0.txt>.
//
// 	You should have received a copy of the GNU General Public License 2.0
// 	along with KSPe API Extensions/L. If not, see <https://www.gnu.org/licenses/>.
//
// */
using System;
using System.Collections.Generic;
using KSP.UI.Screens;
using UnityEngine;

namespace KSPe.UI.Toolbar
{
	public class State
	{
		internal interface Interface
		{
			ApplicationLauncherButton ToolbarController { get; }
		}

		public abstract class Status {
			internal new abstract Type GetType();
			internal Type GetSurrogateType() => base.GetType();
		}
		[Serializable]
		public class Status<T> : Status
		{
			protected readonly T v;
			protected readonly Type myRealType = typeof(T);
			protected Status(T v) { this.v = v; }
			internal override Type GetType() => this.myRealType;
			public override bool Equals(object o) => o is Status<T> && this.v.Equals(((Status<T>)o).v);

			private int _hash = -1;
			public override int GetHashCode()
			{
				if (this._hash > 0) return this._hash;
				int hash = 7;
				hash = 31 * hash + this.myRealType.GetHashCode();
				hash = 31 * hash + this.v.GetHashCode();
				return (this._hash = hash);
			}

			public override string ToString()
			{
				return base.ToString() + ":" + this.v.ToString();
			}
		}

		public class Data
		{
			internal readonly Texture2D largeIcon;
			internal readonly Texture2D smallIcon;

			private Data(Texture2D largeIcon, Texture2D smallIcon)
			{
				this.largeIcon = largeIcon;
				this.smallIcon = smallIcon;
			}

			[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
			public static Data Create(Texture2D largeIcon, Texture2D smallIcon)
			{
				return new Data(largeIcon, smallIcon);
			}
		}

		public class Control
		{
			private readonly Interface owner;
			private readonly Dictionary<Type, Dictionary<Status, Data>> states = new Dictionary<Type, Dictionary<Status, Data>>();
			private Status currentStatus;
			internal Status CurrentStatus
			{
				get => this.currentStatus;
				set { this.currentStatus = value; this.update(); }
			}

			internal bool isEmpty => null == this.currentStatus || 0 == this.states.Count;

			internal Control(Interface owner)
			{
				this.owner = owner;
			}

			public Control Create<T>(Dictionary<Status, Data> data)
			{
				if (!(typeof(T).IsSubclassOf(typeof(Status))))
					throw new InvalidCastException(string.Format("Type {0} is not valid for the operation. It needs to be derived from  KSPe.UI.State.Status<?>!", typeof(T).FullName));

				foreach (Status i in data.Keys) if (i.GetSurrogateType() != typeof(T))
						throw new InvalidCastException(string.Format("Status {0} is not valid for this dataset. It needs to be type {1}", i, typeof(T).FullName));

				if (this.states.ContainsKey(typeof(T))) this.states.Remove(typeof(T));
				this.states.Add(typeof(T), data);
#if DEBUG
			{
				Status[] keys = new Status[data.Keys.Count];
				data.Keys.CopyTo(keys, 0);
				Log.debug("State.Data created for type {0} using Status {1}", typeof(T).FullName, string.Join("; ", Array.ConvertAll(keys, item => item.ToString())));
			}
#endif
				return this;
			}

			internal void update()
			{
				Type t = this.currentStatus.GetSurrogateType();
				if (!this.states.ContainsKey(t)) return;
				Dictionary<Status, Data> dict = this.states[t];

				bool haveTex = dict.ContainsKey(this.currentStatus);
				if (haveTex)
				{
					Data d = dict[this.currentStatus];
					this.owner.ToolbarController.SetTexture(d.largeIcon);
				}
				Log.debug("State.Control update type {0} using {1} {2} texture", t, this.currentStatus, haveTex ? "with" : "without");
			}

			internal void Destroy()
			{
				this.currentStatus = null;
				this.states.Clear();
			}
		}

		private readonly Interface owner;
		internal State(Interface owner)
		{
			this.owner = owner;
			this.controller = new Control(owner);
		}

		private readonly Control controller = null;
		[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
		public Control Controller => this.controller;

		[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
		public State Set(Status value)
		{
			Log.debug("State.Data Set from {0} to {1}", this.controller, value);
			if (value.Equals(this.controller.CurrentStatus)) return this;

			return this.set(value);
		}

		internal State set(Status value)
		{
			this.controller.CurrentStatus = value;
			this.update();
			return this;
		}

		[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
		public State Clear()
		{
			this.controller.Destroy();
			return this;
		}

		internal void update()
		{
			if (null == this.controller) return;
			this.controller.update();
		}
	}

	public class Button : State.Interface
	{
		public class Event
		{
			public delegate void Handler();

			internal readonly Handler raisingEdge;
			internal readonly Handler fallingEdge;

			public Event(Handler raisingEdge, Handler fallingEdge)
			{
				this.raisingEdge = raisingEdge;
				this.fallingEdge = fallingEdge;
			}
		}

		public class MouseEvents
		{
			public enum Kind
			{
				Left,
				Right,
				Middle,
				Scroll
			};

			public class Handler
			{
				public delegate void ClickHandler();
				public delegate float ScrollHandler();

				public readonly Kind kind;
				internal readonly ClickHandler clickHandler;
				internal readonly ScrollHandler scrollHandler;
				internal readonly KeyCode[] modifier;

				internal Handler(Kind kind, ClickHandler handler, KeyCode[] modifier)
				{
					this.kind = kind;
					this.clickHandler = handler;
					this.scrollHandler = null;
					this.modifier = modifier;
				}

				internal Handler(Kind kind, ScrollHandler handler, KeyCode[] modifier)
				{
					this.kind = kind;
					this.clickHandler = null;
					this.scrollHandler = handler;
					this.modifier = modifier;
				}
			}

			internal readonly Dictionary<Kind, Handler> events = new Dictionary<Kind, Handler>();
			private static readonly KeyCode[] EMPTY_MODIFIER = new KeyCode[]{}; 
			internal MouseEvents() { }

			[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
			public MouseEvents Add(Handler handler)
			{
				if (this.events.ContainsKey(handler.kind)) this.events.Remove(handler.kind);
				this.events.Add(handler.kind, handler);
				#if DEBUG
				Log.debug("Button.MouseEvent.Add {0}{1}{2}{3}"
						, handler.kind
						, (null != handler.clickHandler) ? "with clickhandler" : ""
						, (null != handler.scrollHandler) ? "with scrollHandler" : ""
						, (null != handler.modifier) ? "with modifiers" : ""
					);
				#endif
				return this;
			}

			[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
			public MouseEvents Add(MouseEvents.Kind kind, Handler.ClickHandler handler)
			{
				Handler h = new Handler(kind, handler, EMPTY_MODIFIER);
				return this.Add(h);
			}

			[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
			public MouseEvents Add(MouseEvents.Kind kind, Handler.ClickHandler handler, KeyCode[] modifiler)
			{
				Handler h = new Handler(kind, handler, modifiler);
				return this.Add(h);
			}

			[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
			public MouseEvents Add(Handler.ScrollHandler handler)
			{
				Handler h = new Handler(MouseEvents.Kind.Scroll, handler, EMPTY_MODIFIER);
				return this.Add(h);
			}

			[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
			public MouseEvents Add(Handler.ScrollHandler handler, KeyCode[] modifiler)
			{
				Handler h = new Handler(MouseEvents.Kind.Scroll, handler, modifiler);
				return this.Add(h);
			}

			[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
			public Handler this[Kind index] => this.events[index];
			[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
			public bool Has(Kind kind) => this.events.ContainsKey(kind);
		}

		public class ToolbarEvents
		{
			public enum Kind
			{
				Active,		// OnTrue, OnFalse
				Hover,		// HoverIn, HoverOut
				Enabled,	// OnEnable, OnDisable
			};

			private readonly Dictionary<Kind, Event> events = new Dictionary<Kind,Event>();

			internal ToolbarEvents() {	}

			[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
			public ToolbarEvents Add(Kind kind, Event @event)
			{
				if (this.events.ContainsKey(kind)) this.events.Remove(kind);
				this.events.Add(kind, @event);
				#if DEBUG
				Log.debug("Button.ToolbarEvents.Add {0} event{1}{2}"
						, kind
						, (null != @event.raisingEdge) ? " with raisingEdge" : ""
						, (null != @event.fallingEdge) ? " with fallingEdge" : ""
					);
				#endif
				return this;
			}

			[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
			public Event this[Kind index] => this.events[index];
			[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
			public bool Has(Kind kind)
			{
				return this.events.ContainsKey(kind);
			}
		}

		public readonly object owner;
		public readonly string ID;

		internal readonly ApplicationLauncher.AppScenes visibleInScenes;
		internal readonly string toolTip;

		private readonly ToolbarEvents toolbarEvents = new ToolbarEvents();
		[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
		public ToolbarEvents Toolbar => this.toolbarEvents;

		private readonly MouseEvents mouseEvents = new MouseEvents();
		[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
		public MouseEvents Mouse => this.mouseEvents;

		private readonly State state;
		[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
		public State State => this.state;

		[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
		public State.Status Status { get => this.state.Controller.CurrentStatus; set => this.state.Controller.CurrentStatus = value; }

		private ApplicationLauncherButton stockTolbarController;
		[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
		public ApplicationLauncherButton ToolbarController => this.stockTolbarController;

		// A bit messy, but I need the classes to use the Toolbar.States stunt!
		// So, instead of creating 3 dummy classes and then hard code the association with its boolean status,
		// I made the state and the selector class the same! :)
		// (I'm spending too much time programming in Python, I should code a bit more on Java as it appears!!)
		private class ActiveStatus:State.Status<bool>  { protected ActiveStatus(bool v):base(v) { }  public static implicit operator ActiveStatus(bool v) => new ActiveStatus(v);   public static implicit operator bool(ActiveStatus s) => s.v; }
		private class EnabledStatus:State.Status<bool> { protected EnabledStatus(bool v):base(v) { } public static implicit operator EnabledStatus(bool v) => new EnabledStatus(v); public static implicit operator bool(EnabledStatus s) => s.v; }
		private class HoverStatus:State.Status<bool>   { protected HoverStatus(bool v):base(v) { }   public static implicit operator HoverStatus(bool v) => new HoverStatus(v);     public static implicit operator bool(HoverStatus s) => s.v; }
		private ActiveStatus active = false;
		private EnabledStatus enabled = true;
		private HoverStatus hovering = false;

		private Button(
				object owner, string id
				, ApplicationLauncher.AppScenes visibleInScenes
				, string toolTip
			)
		{
			this.owner = owner;
			this.ID = this.owner.GetType().Namespace + "_" + id + "_Button";
			this.visibleInScenes = visibleInScenes;
			this.toolTip = toolTip;
			this.state = new State(this);
			#if DEBUG
			Log.debug("Button Created for {0}, ID {1} for Scenes {2} and toolTip {3}"
					, this.owner, this.ID
					, visibleInScenes
					, toolTip??"<none>"
				);
			#endif
		}

		public override bool Equals(object o)
		{
			if (!(o is Button)) return false;
			Button b = (Button)o;
			return this.owner.Equals(b.owner) && this.ID.Equals(b.ID);
		}

		private int? hash = null;
		public override int GetHashCode()
		{
			if (null != this.hash) return (int)this.hash;
			int hash = 7;
			hash = 31 * hash + this.owner.GetHashCode();
			hash = 31 * hash + this.ID.GetHashCode();
			return (int)(this.hash = hash);
		}

		[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
		public static Button Create(object owner, string id
				, ApplicationLauncher.AppScenes visibleInScenes
				, string toolTip = null
			)
		{
			Button r = new Button(owner, id
					, visibleInScenes
					, toolTip
				);
			return r;
		}

		[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
		public static Button Create(object owner, string id,
				ApplicationLauncher.AppScenes visibleInScenes
				, State.Data iconEnabled, State.Data iconDisabled
				, string toolTip = null
			)
		{
			Button r = new Button(owner, id
					, visibleInScenes
					, toolTip
				);
			r.Add(ToolbarEvents.Kind.Enabled, iconEnabled, iconDisabled);
			return r;
		}

		[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
		public static Button Create(object owner, string id,
				ApplicationLauncher.AppScenes visibleInScenes
				, UnityEngine.Texture2D largeIconEnabled, UnityEngine.Texture2D largeIconDisabled
				, UnityEngine.Texture2D smallIconEnabled, UnityEngine.Texture2D smallIconDisabled
				, string toolTip = null
			)
		{
			return Create(owner, id
					, visibleInScenes
					, State.Data.Create(largeIconEnabled, smallIconEnabled)
					, State.Data.Create(largeIconDisabled, smallIconDisabled)
					, toolTip
				);
		}

		[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
		public static Button Create(object owner
				, ApplicationLauncher.AppScenes visibleInScenes
				, string toolTip = null
			)
		{
			Button r = new Button(owner, owner.GetType().Name
					, visibleInScenes
					, toolTip
				);
			return r;
		}

		[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
		public static Button Create(object owner
				, ApplicationLauncher.AppScenes visibleInScenes
				, UnityEngine.Texture2D largeIconActive, UnityEngine.Texture2D largeIconInactive
				, UnityEngine.Texture2D smallIconActive, UnityEngine.Texture2D smallIconInactive
				, string toolTip = null
			)
		{
			return Create(owner, owner.GetType().Name
					, visibleInScenes
					, State.Data.Create(largeIconActive, smallIconActive)
					, State.Data.Create(largeIconInactive, smallIconInactive)
					, toolTip
				);
		}

		[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
		public static Button Create(object owner
				, ApplicationLauncher.AppScenes visibleInScenes
				, UnityEngine.Texture2D largeIcon
				, UnityEngine.Texture2D smallIcon
				, string toolTip = null
			)
		{
			return Create(owner, owner.GetType().Name
					, visibleInScenes
					, State.Data.Create(largeIcon, smallIcon)
					, State.Data.Create(largeIcon, smallIcon)
					, toolTip
				);
		}

		[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
		public bool Active
		{
			get { return this.active; }
			set { this.updateActiveState(value); }
		}

		private bool Hovering
		{
			get { return this.hovering; }
			set { this.updateHoverState(value); }
		}

		[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
		public bool Enabled
		{
			get { return this.enabled; }
			set { this.updateEnableState(value); }
		}

		[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
		public void Add(ToolbarEvents.Kind kind, State.Data iconActive, State.Data iconInactive)
		{
			switch (kind)
			{
				case ToolbarEvents.Kind.Active:
					this.state.Controller.Create<ActiveStatus>(
						new Dictionary<State.Status, State.Data> {
							{ (ActiveStatus)false, iconInactive }, { (ActiveStatus)true, iconActive }
						})
					;
					break;
				case ToolbarEvents.Kind.Hover:
					this.state.Controller.Create<HoverStatus>(
						new Dictionary<State.Status, State.Data> {
							{ (HoverStatus)false, iconInactive }, { (HoverStatus)true, iconActive }
						})
					;
					break;
				case ToolbarEvents.Kind.Enabled:
					this.state.Controller.Create<EnabledStatus>(
						new Dictionary<State.Status, State.Data> {
							{ (EnabledStatus)false, iconInactive }, { (EnabledStatus)true, iconActive }
						})
					;
					break;
				default:
					break;
			}
		}

		[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
		public Vector3 GetAnchor() => this.stockTolbarController.GetAnchor(); // FIXME: What I return when I'm on Blizzy mode?

		internal void set(ApplicationLauncherButton applicationLauncherButton)
		{
			this.stockTolbarController = applicationLauncherButton;
			this.stockTolbarController.onLeftClick = this.OnLeftClick;
			this.stockTolbarController.onRightClick = this.OnRightClick;
			this.state.set(this.enabled = true);
		}

		internal void clear()
		{
			this.stockTolbarController = null;
		}

		internal void OnTrue()
		{
			if (!this.enabled)	return;
			this.sendRaisingEvent(ToolbarEvents.Kind.Active);
			this.active = true;
			this.state.set(this.active);	// Now do you see why that mess above? ;)
		}

		internal void OnFalse()
		{
			if (!this.enabled)	return;
			this.sendFallingEvent(ToolbarEvents.Kind.Active);
			this.active = false;
			this.state.set(this.active);	// Now do you see why that mess above? ;)
		}

		internal void OnHoverIn()
		{
			if (!this.enabled)
			{ 
				if (this.hovering)
				{
					this.sendFallingEvent(ToolbarEvents.Kind.Hover);
					this.hovering = false;
				}
				return;
			}

			this.sendRaisingEvent(ToolbarEvents.Kind.Hover);
			this.hovering = true;
			this.state.set(this.hovering);	// Now do you see why that mess above? ;)
		}

		internal void OnHoverOut()
		{
			if (!this.enabled)	return;
			this.sendFallingEvent(ToolbarEvents.Kind.Hover);
			this.hovering = false;
			this.state.set(this.hovering);	// Now do you see why that mess above? ;)
		}

		internal void OnEnable()
		{
			this.sendRaisingEvent(ToolbarEvents.Kind.Enabled);
			this.enabled = true;
			this.state.set(this.enabled);	// Now do you see why that mess above? ;)
		}

		internal void OnDisable()
		{
			if (this.hovering)	this.sendFallingEvent(ToolbarEvents.Kind.Hover);
			if (this.active)	this.sendFallingEvent(ToolbarEvents.Kind.Active);

			this.sendFallingEvent(ToolbarEvents.Kind.Enabled);
			this.active = false;
			this.hovering = false;
			this.enabled = false;
			this.state.set(this.enabled);	// Now do you see why that mess above? ;)
		}

		private void OnLeftClick()
		{
			if (!this.enabled)	return;
			if (!this.mouseEvents.Has(MouseEvents.Kind.Left)) return;
			this.HandleMouseClick(MouseEvents.Kind.Left);
		}

		private void OnRightClick()
		{
			if (!this.enabled)	return;
			if (!this.mouseEvents.Has(MouseEvents.Kind.Right)) return;
			this.HandleMouseClick(MouseEvents.Kind.Right);
		}

		private void HandleMouseClick(MouseEvents.Kind kind)
		{
			if (!this.enabled)	return;
			foreach (KeyCode k in this.mouseEvents[kind].modifier)
				if (!UnityEngine.Input.GetKeyDown(k)) return;
			this.mouseEvents[kind].clickHandler();
		}

		private void updateActiveState(bool newState)
		{
			if (newState == this.active) return;
			#if DEBUG
			Log.debug("Button {0}::{1} updateActiveState set to {2}"
					, this.owner, this.ID
					, newState
				);
			#endif
			if (this.active) this.OnFalse(); else this.OnTrue();
		}

		private void updateHoverState(bool newState)
		{
			if (newState == this.hovering) return;
			#if DEBUG
			Log.debug("Button {0}::{1} updateHoverState set to {2}"
					, this.owner, this.ID
					, newState
				);
			#endif
			if (this.hovering) this.OnHoverOut(); else this.OnHoverIn();
		}

		private void updateEnableState(bool newState)
		{
			if (newState == this.enabled) return;
			#if DEBUG
			Log.debug("Button {0}::{1} updateEnableState set to {2}"
					, this.owner, this.ID
					, newState
				);
			#endif
			if (this.enabled) this.OnDisable(); else this.OnEnable();
		}

		private void sendRaisingEvent(ToolbarEvents.Kind kind)
		{
			if (this.toolbarEvents.Has(kind) && null != this.toolbarEvents[kind].raisingEdge)
				this.toolbarEvents[kind].raisingEdge();
		}

		private void sendFallingEvent(ToolbarEvents.Kind kind)
		{
			if (this.toolbarEvents.Has(kind) && null != this.toolbarEvents[kind].fallingEdge)
				this.toolbarEvents[kind].fallingEdge();
		}

	}

	public class Toolbar
	{
		private readonly Type type;
		private readonly string displayName;
		private readonly List<Button> buttons = new List<Button>();

		public Toolbar(Type type, string displayName)
		{
			this.type = type;
			this.displayName = displayName ?? type.Namespace;
		}

		/**
		 * I know that you know that everybody knows that people will forget to call the Destroy
		 * on the Destroy of their MonoBehaviours...
		 * 
		 * It's still bad doing it here, but it's less worse than not doing it at all!
		 * 
		 * Calling Destroy twice will not hurt anyway.
		 */
		~Toolbar()
		{
			this.Destroy();
		}

		public void Destroy()
		{
			foreach(Button b in this.buttons) ApplicationLauncher.Instance.RemoveModApplication(b.ToolbarController);
			this.buttons.Clear();
		}

		[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
		public bool BlizzyActive(bool? useBlizzy = null)
		{
			return false;
		}

		[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
		public bool StockActive(bool? useStock = null)
		{
			return true;
		}

		[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
		public void ButtonsActive(bool? useStock, bool? useBlizzy)
		{
		}

		private static Texture2D defaultButton = null;
		[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
		public Toolbar Add(Button button)
		{
			if (null == defaultButton) try
			{
				using (System.IO.Stream si = this.GetType().Assembly.GetManifestResourceStream("KSPe.UI.Resources.launchicon"))
				{ 
					Byte[] data = new System.IO.BinaryReader(si).ReadBytes(int.MaxValue);
					if (!KSPe.Util.Image.File.Load(out defaultButton, data))
						throw new NullReferenceException("KSPe.UI.Resources.launchicon");	// Screw the best practices, I will not rework ths just because of them.
				}
			}
			catch (Exception e)
			{
				Log.error(e, "Could not read the Default Button texture from Resources.");
				defaultButton = new Texture2D(16, 16);
			}

			if (this.buttons.Contains(button))
			{
				this.buttons.Remove(button);
				ApplicationLauncher.Instance.RemoveModApplication(button.ToolbarController);
				button.clear();
			}
			this.buttons.Add(button);
			button.set(
				ApplicationLauncher.Instance.AddModApplication(
					button.OnTrue, button.OnFalse
					, button.OnHoverIn, button.OnHoverOut
					, button.OnEnable, button.OnDisable
					, button.visibleInScenes
					, defaultButton
				)
			);
			return this;
		}

		[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
		public bool Contains(Button button) => this.buttons.Contains(button);
	}

	public class Controller
	{
		private static Controller instance = null;
		public static Controller Instance = instance ?? (instance = new Controller());

		private readonly Dictionary<Type, Toolbar> toolbars = new Dictionary<Type, Toolbar>();

		[Obsolete("Toobar Support is still alpha. Be aware that interfaces and contracts can break between releases. KSPe suggests to wait until v2.4.2.0 before using it on your plugins.")]
		public bool Register<T>(string displayName = null, bool useBlizzy = false, bool useStock = true, bool NoneAllowed = true)
		{
			Log.info("Toolbar is registering {0} with type {1}", displayName??typeof(T).Namespace, typeof(T));
			this.toolbars.Add(typeof(T), new Toolbar(typeof(T), displayName));
			return true;
		}

		public Toolbar Get<T>() => this.toolbars[typeof(T)];
	}

}