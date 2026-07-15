using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Server.Commands;

namespace Server
{
	public static class HotReloadManager
	{
		// Soft reload: compile a new script assembly and switch future type lookups to it.
		// Existing Item/Mobile instances and already-registered delegates remain old CLR objects
		// unless a script explicitly handles that through HotUnload/HotReload hooks.
		private static readonly object m_SyncRoot = new object();
		private static readonly TimeSpan m_Debounce = TimeSpan.FromSeconds( 2.0 );

		private static bool m_Started;
		private static bool m_Reloading;
		private static bool m_Pending;
		private static DateTime m_ReloadAfter;
		private static string m_PendingReason;
		private static Mobile m_PendingRequester;
		private static List<FileSystemWatcher> m_Watchers;

		public static bool Started { get { return m_Started; } }
		public static bool Reloading { get { return m_Reloading; } }
		public static bool Pending { get { return m_Pending; } }

		public static void Start()
		{
			if ( m_Started )
				return;

			m_Started = true;
			m_Watchers = new List<FileSystemWatcher>();

			RegisterCommand();
			StartWatcher( Path.Combine( Core.BaseDirectory, "Info/Scripts" ) );
			StartWatcher( Path.Combine( Core.BaseDirectory, "Source/Scripts" ) );

			Core.Slice += Slice;

			Console.WriteLine( "HotReload: enabled. Watching script changes; use [HotReload to compile immediately." );
			Console.WriteLine( "HotReload: use [HotReload status for queue state, or [HotReload help for hook details." );
			Console.WriteLine( "HotReload: live Item/Mobile instances keep their current runtime type until respawn or restart." );
		}

		private static void RegisterCommand()
		{
			CommandSystem.Register( "HotReload", AccessLevel.Developer, new CommandEventHandler( HotReload_OnCommand ) );
			CommandSystem.Register( "ReloadScripts", AccessLevel.Developer, new CommandEventHandler( HotReload_OnCommand ) );
		}

		private static void StartWatcher( string path )
		{
			if ( !Directory.Exists( path ) )
				return;

			FileSystemWatcher watcher = new FileSystemWatcher( path, "*.cs" );
			watcher.IncludeSubdirectories = true;
			watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime;
			watcher.Changed += OnScriptChanged;
			watcher.Created += OnScriptChanged;
			watcher.Deleted += OnScriptChanged;
			watcher.Renamed += OnScriptRenamed;
			watcher.EnableRaisingEvents = true;

			m_Watchers.Add( watcher );
		}

		private static void OnScriptRenamed( object sender, RenamedEventArgs e )
		{
			ScheduleReload( String.Format( "renamed {0}", SafeRelativePath( e.FullPath ) ), null, m_Debounce );
		}

		private static void OnScriptChanged( object sender, FileSystemEventArgs e )
		{
			ScheduleReload( String.Format( "{0} {1}", e.ChangeType, SafeRelativePath( e.FullPath ) ), null, m_Debounce );
		}

		private static string SafeRelativePath( string path )
		{
			try
			{
				string full = Path.GetFullPath( path );
				string root = Path.GetFullPath( Core.BaseDirectory );

				if ( full.StartsWith( root, StringComparison.OrdinalIgnoreCase ) )
					return full.Substring( root.Length ).TrimStart( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar );
			}
			catch
			{
			}

			return path;
		}

		private static void HotReload_OnCommand( CommandEventArgs e )
		{
			string arg = e.ArgString == null ? String.Empty : e.ArgString.Trim();

			if ( Insensitive.Equals( arg, "status" ) )
			{
				if ( m_Reloading )
					e.Mobile.SendMessage( "HotReload is currently compiling." );
				else if ( m_Pending )
					e.Mobile.SendMessage( "HotReload is queued: {0}", m_PendingReason );
				else
					e.Mobile.SendMessage( "HotReload is idle." );

				return;
			}

			if ( Insensitive.Equals( arg, "help" ) )
			{
				e.Mobile.SendMessage( "Usage: [HotReload, [ReloadScripts, [HotReload status." );
				e.Mobile.SendMessage( "Optional public static HotUnload/HotReload methods are invoked when present." );
				e.Mobile.SendMessage( "Live Item/Mobile instances keep their old CLR type until respawn or restart." );
				return;
			}

			ScheduleReload( "manual reload", e.Mobile, TimeSpan.Zero );
		}

		private static void ScheduleReload( string reason, Mobile requester, TimeSpan delay )
		{
			lock ( m_SyncRoot )
			{
				if ( m_Reloading )
				{
					m_Pending = true;
					m_PendingReason = reason;
					m_PendingRequester = requester;
					m_ReloadAfter = DateTime.Now + delay;
					return;
				}

				m_Pending = true;
				m_PendingReason = reason;
				m_PendingRequester = requester;
				m_ReloadAfter = DateTime.Now + delay;
			}

			Core.Set();
		}

		private static void Slice()
		{
			if ( Core.Closing )
				return;

			string reason;
			Mobile requester;

			lock ( m_SyncRoot )
			{
				if ( !m_Pending || m_Reloading || DateTime.Now < m_ReloadAfter )
					return;

				m_Pending = false;
				reason = m_PendingReason;
				requester = m_PendingRequester;
				m_PendingReason = null;
				m_PendingRequester = null;
				m_Reloading = true;
			}

			try
			{
				Reload( reason, requester );
			}
			finally
			{
				lock ( m_SyncRoot )
				{
					m_Reloading = false;
				}
			}
		}

		private static void Reload( string reason, Mobile requester )
		{
			if ( World.Saving || World.Loading )
			{
				ScheduleReload( reason, requester, TimeSpan.FromSeconds( 5.0 ) );
				Notify( requester, "HotReload deferred while the world is saving or loading." );
				return;
			}

			Assembly[] oldAssemblies = ScriptCompiler.Assemblies;
			DateTime start = DateTime.Now;

			Console.WriteLine( "HotReload: compiling scripts ({0})...", reason );
			Notify( requester, "HotReload: compiling scripts..." );

			bool compiled;

			try
			{
				compiled = ScriptCompiler.Compile( Core.Debug, false );
			}
			catch ( Exception ex )
			{
				ScriptCompiler.Assemblies = oldAssemblies;
				ScriptCompiler.ClearTypeCaches();

				Console.WriteLine( "HotReload: compile threw an exception:" );
				Console.WriteLine( ex );
				Notify( requester, "HotReload failed: compile exception. Check the console." );
				return;
			}

			if ( !compiled )
			{
				ScriptCompiler.Assemblies = oldAssemblies;
				ScriptCompiler.ClearTypeCaches();

				Console.WriteLine( "HotReload: compile failed; keeping the previous script assembly active." );
				Notify( requester, "HotReload failed: compile errors. The old scripts are still active." );
				return;
			}

			ScriptCompiler.ClearTypeCaches();

			int unloadHooks = InvokeHook( oldAssemblies, "HotUnload" );
			int reloadHooks = InvokeHook( ScriptCompiler.Assemblies, "HotReload" );

			TimeSpan elapsed = DateTime.Now - start;
			Console.WriteLine( "HotReload: compile complete in {0:F2}s. HotUnload hooks: {1}; HotReload hooks: {2}.", elapsed.TotalSeconds, unloadHooks, reloadHooks );
			Console.WriteLine( "HotReload: existing world objects still use their original CLR type; respawn or restart to replace them." );

			Notify( requester, "HotReload complete in {0:F2}s. New type lookups use the new assembly.", elapsed.TotalSeconds );
		}

		private static int InvokeHook( Assembly[] assemblies, string method )
		{
			if ( assemblies == null )
				return 0;

			List<MethodInfo> invoke = new List<MethodInfo>();

			for ( int a = 0; a < assemblies.Length; ++a )
			{
				if ( assemblies[a] == null )
					continue;

				Type[] types;

				try
				{
					types = assemblies[a].GetTypes();
				}
				catch ( ReflectionTypeLoadException ex )
				{
					types = ex.Types;
				}

				for ( int i = 0; i < types.Length; ++i )
				{
					Type type = types[i];

					if ( type == null )
						continue;

					MethodInfo m = type.GetMethod( method, BindingFlags.Static | BindingFlags.Public );

					if ( m != null && m.GetParameters().Length == 0 )
						invoke.Add( m );
				}
			}

			invoke.Sort( new CallPriorityComparer() );

			for ( int i = 0; i < invoke.Count; ++i )
			{
				try
				{
					invoke[i].Invoke( null, null );
				}
				catch ( Exception ex )
				{
					Console.WriteLine( "HotReload: {0}.{1} failed:", invoke[i].DeclaringType.FullName, method );
					Console.WriteLine( ex );
				}
			}

			return invoke.Count;
		}

		private static void Notify( Mobile mobile, string message, params object[] args )
		{
			if ( mobile == null || mobile.Deleted )
				return;

			if ( args != null && args.Length > 0 )
				mobile.SendMessage( message, args );
			else
				mobile.SendMessage( message );
		}
	}
}