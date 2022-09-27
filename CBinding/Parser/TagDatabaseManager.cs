﻿////
//// TagDatabaseManager.cs
////
//// Authors:
////   Marcos David Marin Amador <MarcosMarin@gmail.com>
////   Mitchell Wheeler <mitchell.wheeler@gmail.com>
////
//// Copyright (C) 2007 Marcos David Marin Amador
////
////
//// This source code is licenced under The MIT License:
////
//// Permission is hereby granted, free of charge, to any person obtaining
//// a copy of this software and associated documentation files (the
//// "Software"), to deal in the Software without restriction, including
//// without limitation the rights to use, copy, modify, merge, publish,
//// distribute, sublicense, and/or sell copies of the Software, and to
//// permit persons to whom the Software is furnished to do so, subject to
//// the following conditions:
//// 
//// The above copyright notice and this permission notice shall be
//// included in all copies or substantial portions of the Software.
//// 
//// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
//// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
//// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
////

//using System;
//using System.IO;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Collections.Generic;
//using System.Threading;

//using MonoDevelop.Projects;
//using MonoDevelop.Core;
//using MonoDevelop.Core.Execution;
//using MonoDevelop.Ide.Gui;

//using CBinding.Navigation;
//using MonoDevelop.Ide.TypeSystem;

////  TODO
////  Generic, language independant 'TagDatabaseManager'
////  Parsing of ctags data into a Sqlite database, for easy/efficient access & updates.
////  
//namespace CBinding
//{
//	public enum TagKind
//	{
//		Class = 'c',
//		Macro = 'd',
//		Enumerator = 'e',
//		Function = 'f',
//		Enumeration = 'g',
//		Local = 'l',
//		Member = 'm',
//		Namespace = 'n',
//		Prototype = 'p',
//		Structure = 's',
//		Typedef = 't',
//		Union = 'u',
//		Variable = 'v',
//		ExternalVariable = 'x',
//		Unknown = ' '
//	}

//	public enum AccessModifier
//	{
//		Private,
//		Protected,
//		Public
//	}

//	/// <summary>
//	/// Singleton class to manage tag databases
//	/// </summary>
//	class TagDatabaseManager
//	{
//		private static TagDatabaseManager instance;
//		private Queue<ProjectFilePair> parsingJobs = new Queue<ProjectFilePair> ();
//		private Thread parsingThread;

//		public event ClassPadEventHandler FileUpdated;

//		bool ctagsInstalled = false;
//		bool checkedCtagsInstalled = false;

//		private TagDatabaseManager ()
//		{
//		}

//		public static TagDatabaseManager Instance {
//			get {
//				if (instance == null)
//					instance = new TagDatabaseManager ();

//				return instance;
//			}
//		}

//		bool DepsInstalled {
//			get {
//				if (!checkedCtagsInstalled) {
//					checkedCtagsInstalled = true;
//					return false;


//					try {
//						Runtime.ProcessService.StartProcess ("ctags", "--version", null, null).WaitForOutput ();
//					} catch {
//						LoggingService.LogWarning ("Cannot update C/C++ tags database because exuberant ctags is not installed.");
//						return false;
//					}
//					try {
//						Runtime.ProcessService.StartProcess ("gcc", "--version", null, null).WaitForOutput ();
//					} catch {
//						LoggingService.LogWarning ("Cannot update C/C++ tags database because gcc is not installed.");
//						return false;
//					}
//					lock (parsingJobs) {
//						ctagsInstalled = true;
//					}
//				}
//				return ctagsInstalled;
//			}
//			set {
//				//don't assume that the caller is correct :-)
//				if (value)
//					checkedCtagsInstalled = false; //wil re-determine ctagsInstalled on next getting
//				else
//					ctagsInstalled = false;
//			}
//		}

//		private string [] Headers (Project project, string filename, bool with_system)
//		{
//			List<string> headers = new List<string> ();
//			CProject cproject = project as CProject;
//			if (cproject == null) { return headers.ToArray (); }

//			StringBuilder output = new StringBuilder ();
//			StringBuilder option = new StringBuilder ("-M");
//			if (!with_system) {
//				option.Append ("M");
//			}

//			option.Append (" -MG ");
//			foreach (Package package in cproject.Packages) {
//				package.ParsePackage ();
//				option.AppendFormat ("{0} ", string.Join (" ", package.CFlags.ToArray ()));
//			}

//			ProcessWrapper p = null;
//			try {
//				p = Runtime.ProcessService.StartProcess ("gcc", option.ToString () + filename.Replace (@"\ ", " ").Replace (" ", @"\ "), null, null);
//				p.WaitForOutput ();

//				// Doing the below completely breaks header parsing
//				// // Skip first two lines (.o & .c* files) - WARNING, sometimes this is compacted to 1 line... we need a better way of handling this.
//				// if(p.StandardOutput.ReadLine () == null) return new string[0]; // object file
//				// if(p.StandardOutput.ReadLine () == null) return new string[0]; // compile file

//				string line;
//				while ((line = p.StandardOutput.ReadLine ()) != null)
//					output.Append (line);
//			} catch (Exception ex) {
//				LoggingService.LogError (ex.ToString ());
//				return new string [0];
//			} finally {
//				if (p != null)
//					p.Dispose ();
//			}

//			MatchCollection files = Regex.Matches (output.ToString ().Replace (@" \", String.Empty), @" (?<file>([^ \\]|(\\ ))+)", RegexOptions.IgnoreCase);

//			foreach (Match match in files) {
//				string depfile = findFileInIncludes (project, match.Groups ["file"].Value.Trim ());

//				headers.Add (depfile.Replace (@"\ ", " ").Replace (" ", @"\ "));
//			}

//			return headers.ToArray ();
//		}

//		/// <summary>
//		/// Finds a file in a project's include path(s)
//		/// </summary>
//		/// <param name="project">
//		/// The project whose include path is to be searched
//		/// <see cref="Project"/>
//		/// </param>
//		/// <param name="filename">
//		/// A portion of a full file path
//		/// <see cref="System.String"/>
//		/// </param>
//		/// <returns>
//		/// The full found path, or filename if not found
//		/// <see cref="System.String"/>
//		/// </returns>
//		private static string findFileInIncludes (Project project, string filename)
//		{
//			CProjectConfiguration conf = project.DefaultConfiguration as CProjectConfiguration;
//			string fullpath = string.Empty;

//			if (!Path.IsPathRooted (filename)) {
//				// Check against base directory
//				fullpath = findFileInPath (filename, project.BaseDirectory);
//				if (string.Empty != fullpath) return fullpath;

//				// Check project's additional configuration includes
//				foreach (string p in conf.Includes) {
//					fullpath = findFileInPath (filename, p);
//					if (string.Empty != fullpath) return fullpath;
//				}
//			}

//			return filename;
//		}

//		/// <summary>
//		/// Finds a file in a subdirectory of a given path
//		/// </summary>
//		/// <param name="relativeFilename">
//		/// A portion of a full file path
//		/// <see cref="System.String"/>
//		/// </param>
//		/// <param name="path">
//		/// The path beneath which to look for relativeFilename
//		/// <see cref="System.String"/>
//		/// </param>
//		/// <returns>
//		/// The full path, or string.Empty if not found
//		/// <see cref="System.String"/>
//		/// </returns>
//		private static string findFileInPath (string relativeFilename, string path)
//		{
//			string tmp = Path.Combine (path, relativeFilename);

//			if (Path.IsPathRooted (relativeFilename))
//				return relativeFilename;
//			else if (File.Exists (tmp))
//				return tmp;

//			if (Directory.Exists (path)) {
//				foreach (string subdir in Directory.GetDirectories (path)) {
//					tmp = findFileInPath (relativeFilename, subdir);
//					if (string.Empty != tmp) return tmp;
//				}
//			}

//			return string.Empty;
//		}

//		private void UpdateSystemTags (Project project, string filename, string [] includedFiles)
//		{
//			ProjectInformation info = ProjectInformationManager.Instance.Get (project);
//			List<FileInformation> files;

//			lock (info) {
//				if (!info.IncludedFiles.ContainsKey (filename)) {
//					files = new List<FileInformation> ();
//					info.IncludedFiles.Add (filename, files);
//				} else {
//					files = info.IncludedFiles [filename];
//				}

//				foreach (string includedFile in includedFiles) {
//					bool contains = false;

//					foreach (FileInformation fi in files) {
//						if (fi.FileName == includedFile) {
//							contains = true;
//						}
//					}

//					if (!contains) {
//						FileInformation newFileInfo = new FileInformation (project, includedFile);
//						files.Add (newFileInfo);
//						FillFileInformation (newFileInfo);
//					}

//					contains = false;
//				}
//			}
//		}

//		private void FillFileInformation (FileInformation fileInfo)
//		{
//			if (!DepsInstalled)
//				return;

//			string confdir = PropertyService.ConfigPath;
//			string tagFileName = Path.GetFileName (fileInfo.FileName) + ".tag";
//			string tagdir = Path.Combine (confdir, "system-tags");
//			string tagFullFileName = Path.Combine (tagdir, tagFileName);
//			string ctags_kinds = "--C++-kinds=+px";

//			if (PropertyService.Get<bool> ("CBinding.ParseLocalVariables", true))
//				ctags_kinds += "l";

//			string ctags_options = ctags_kinds + " --fields=+aStisk-fz --language-force=C++ --excmd=number --line-directives=yes -f '" + tagFullFileName + "' '" + fileInfo.FileName + "'";

//			if (!Directory.Exists (tagdir))
//				Directory.CreateDirectory (tagdir);

//			if (!File.Exists (tagFullFileName) || File.GetLastWriteTimeUtc (tagFullFileName) < File.GetLastWriteTimeUtc (fileInfo.FileName)) {
//				ProcessWrapper p = null;
//				System.IO.StringWriter output = null;
//				try {
//					output = new System.IO.StringWriter ();

//					p = Runtime.ProcessService.StartProcess ("ctags", ctags_options, null, null, output, null);
//					p.WaitForOutput (10000);
//					if (p.ExitCode != 0 || !File.Exists (tagFullFileName)) {
//						LoggingService.LogError ("Ctags did not successfully populate the tags database '{0}' within ten seconds.\nOutput: {1}", tagFullFileName, output.ToString ());
//						return;
//					}
//				} catch (Exception ex) {
//					throw new IOException ("Could not create tags database (You must have exuberant ctags installed).", ex);
//				} finally {
//					if (output != null)
//						output.Dispose ();
//					if (p != null)
//						p.Dispose ();
//				}
//			}

//			string ctags_output;
//			string tagEntry;

//			using (StreamReader reader = new StreamReader (tagFullFileName)) {
//				ctags_output = reader.ReadToEnd ();
//			}

//			using (StringReader reader = new StringReader (ctags_output)) {
//				while ((tagEntry = reader.ReadLine ()) != null) {
//					if (tagEntry.StartsWith ("!_")) continue;

//					Tag tag = ParseTag (tagEntry);

//					if (tag != null)
//						AddInfo (fileInfo, tag, ctags_output);
//				}
//			}

//			fileInfo.IsFilled = true;
//		}

//		private void ParsingThread ()
//		{
//			try {
//				while (parsingJobs.Count > 0) {
//					ProjectFilePair p;

//					lock (parsingJobs) {
//						p = parsingJobs.Dequeue ();
//					}

//					DoUpdateFileTags (p.Project, p.File);
//				}
//			} catch (Exception ex) {
//				LoggingService.LogError ("Unhandled error updating parser database. Disabling C/C++ parsing.", ex);
//				DepsInstalled = false;
//				return;
//			}
//			//			catch {
//			//				LoggingService.LogError("Unexpected error while updating parser database. Disabling C/C++ parsing.");
//			//				DepsInstalled = false;
//			//			}
//		}

//		public void UpdateFileTags (Project project, string filename)
//		{
//			if (!DepsInstalled)
//				return;

//			ProjectFilePair p = new ProjectFilePair (project, filename);

//			lock (parsingJobs) {
//				if (!parsingJobs.Contains (p))
//					parsingJobs.Enqueue (p);
//			}

//			if (parsingThread == null || !parsingThread.IsAlive) {
//				parsingThread = new Thread (ParsingThread);
//				parsingThread.Name = "Tag database parser";
//				parsingThread.IsBackground = true;
//				parsingThread.Priority = ThreadPriority.Lowest;
//				parsingThread.Start ();
//			}
//		}

//		private void DoUpdateFileTags (Project project, string filename)
//		{
//			if (!DepsInstalled)
//				return;

//			string [] headers = Headers (project, filename, false);
//			string [] system_headers = diff (Headers (project, filename, true), headers);
//			StringBuilder ctags_kinds = new StringBuilder ("--C++-kinds=+px");

//			if (PropertyService.Get<bool> ("CBinding.ParseLocalVariables", true))
//				ctags_kinds.Append ("+l");

//			// Maybe we should only ask for locals for 'local' files? (not external #includes?)
//			ctags_kinds.AppendFormat (" --fields=+aStisk-fz --language-force=C++ --excmd=number --line-directives=yes -f - '{0}'", filename);
//			foreach (string header in headers) {
//				ctags_kinds.AppendFormat (" '{0}'", header);
//			}

//			string ctags_output = string.Empty;

//			ProcessWrapper p = null;
//			System.IO.StringWriter output = null, error = null;
//			try {
//				output = new System.IO.StringWriter ();
//				error = new System.IO.StringWriter ();

//				p = Runtime.ProcessService.StartProcess ("ctags", ctags_kinds.ToString (), project.BaseDirectory, output, error, null);
//				p.WaitForOutput (10000);
//				if (p.ExitCode != 0) {
//					LoggingService.LogError ("Ctags did not successfully populate the tags database from '{0}' within ten seconds.\nError output: {1}", filename, error.ToString ());
//					return;
//				}
//				ctags_output = output.ToString ();
//			} catch (Exception ex) {
//				throw new IOException ("Could not create tags database (You must have exuberant ctags installed).", ex);
//			} finally {
//				if (output != null)
//					output.Dispose ();
//				if (error != null)
//					error.Dispose ();
//				if (p != null)
//					p.Dispose ();
//			}

//			ProjectInformation info = ProjectInformationManager.Instance.Get (project);

//			lock (info) {
//				info.RemoveFileInfo (filename);
//				string tagEntry;

//				using (StringReader reader = new StringReader (ctags_output)) {
//					while ((tagEntry = reader.ReadLine ()) != null) {
//						if (tagEntry.StartsWith ("!_")) continue;

//						Tag tag = ParseTag (tagEntry);

//						if (tag != null)
//							AddInfo (info, tag, ctags_output);
//					}
//				}
//			}

//			OnFileUpdated (new ClassPadEventArgs (project));

//			if (PropertyService.Get<bool> ("CBinding.ParseSystemTags", true))
//				UpdateSystemTags (project, filename, system_headers);

//			if (cache.Count > cache_size)
//				cache.Clear ();
//		}

//		private void AddInfo (FileInformation info, Tag tag, string ctags_output)
//		{
//			switch (tag.Kind) {
//			case TagKind.Class:
//				Class c = new Class (tag, info.Project, ctags_output);
//				if (!info.Classes.Contains (c))
//					info.Classes.Add (c);
//				break;
//			case TagKind.Enumeration:
//				Enumeration e = new Enumeration (tag, info.Project, ctags_output);
//				if (!info.Enumerations.Contains (e))
//					info.Enumerations.Add (e);
//				break;
//			case TagKind.Enumerator:
//				Enumerator en = new Enumerator (tag, info.Project, ctags_output);
//				if (!info.Enumerators.Contains (en))
//					info.Enumerators.Add (en);
//				break;
//			case TagKind.ExternalVariable:
//				break;
//			case TagKind.Function:
//				Function f = new Function (tag, info.Project, ctags_output);
//				if (!info.Functions.Contains (f))
//					info.Functions.Add (f);
//				break;
//			case TagKind.Local:
//				Local lo = new Local (tag, info.Project, ctags_output);
//				if (!info.Locals.Contains (lo))
//					info.Locals.Add (lo);
//				break;
//			case TagKind.Macro:
//				Macro m = new Macro (tag, info.Project);
//				if (!info.Macros.Contains (m))
//					info.Macros.Add (m);
//				break;
//			case TagKind.Member:
//				Member me = new Member (tag, info.Project, ctags_output);
//				if (!info.Members.Contains (me))
//					info.Members.Add (me);
//				break;
//			case TagKind.Namespace:
//				Namespace n = new Namespace (tag, info.Project, ctags_output);
//				if (!info.Namespaces.Contains (n))
//					info.Namespaces.Add (n);
//				break;
//			case TagKind.Prototype:
//				Function fu = new Function (tag, info.Project, ctags_output);
//				if (!info.Functions.Contains (fu))
//					info.Functions.Add (fu);
//				break;
//			case TagKind.Structure:
//				Structure s = new Structure (tag, info.Project, ctags_output);
//				if (!info.Structures.Contains (s))
//					info.Structures.Add (s);
//				break;
//			case TagKind.Typedef:
//				Typedef t = new Typedef (tag, info.Project, ctags_output);
//				if (!info.Typedefs.Contains (t))
//					info.Typedefs.Add (t);
//				break;
//			case TagKind.Union:
//				Union u = new Union (tag, info.Project, ctags_output);
//				if (!info.Unions.Contains (u))
//					info.Unions.Add (u);
//				break;
//			case TagKind.Variable:
//				Variable v = new Variable (tag, info.Project);
//				if (!info.Variables.Contains (v))
//					info.Variables.Add (v);
//				break;
//			default:
//				break;
//			}
//		}

//		private Tag ParseTag (string tagEntry)
//		{
//			string file;
//			UInt64 line;
//			string name;
//			string tagField;
//			TagKind kind;
//			AccessModifier access = AccessModifier.Public;
//			string _class = null;
//			string _namespace = null;
//			string _struct = null;
//			string _union = null;
//			string _enum = null;
//			string signature = null;

//			int i1 = tagEntry.IndexOf ('\t');
//			name = tagEntry.Substring (0, tagEntry.IndexOf ('\t'));

//			i1 += 1;
//			int i2 = tagEntry.IndexOf ('\t', i1);
//			file = tagEntry.Substring (i1, i2 - i1);

//			i1 = i2 + 1;
//			i2 = tagEntry.IndexOf (";\"", i1);
//			line = UInt64.Parse (tagEntry.Substring (i1, i2 - i1));

//			i1 = i2 + 3;
//			kind = (TagKind)tagEntry [i1];

//			i1 += 2;
//			tagField = (tagEntry.Length > i1 ? tagField = tagEntry.Substring (i1) : String.Empty);

//			string [] fields = tagField.Split ('\t');
//			int index;

//			foreach (string field in fields) {
//				index = field.IndexOf (':');

//				// TODO: Support friend modifier
//				if (index > 0) {
//					string key = field.Substring (0, index);
//					string val = field.Substring (index + 1);

//					switch (key) {
//					case "access":
//						try {
//							access = (AccessModifier)System.Enum.Parse (typeof (AccessModifier), val, true);
//						} catch (ArgumentException) {
//						}
//						break;
//					case "class":
//						_class = val;
//						break;
//					case "namespace":
//						_namespace = val;
//						break;
//					case "struct":
//						_struct = val;
//						break;
//					case "union":
//						_union = val;
//						break;
//					case "enum":
//						_enum = val;
//						break;
//					case "signature":
//						signature = val;
//						break;
//					}
//				}
//			}

//			return new Tag (name, file, line, kind, access, _class, _namespace, _struct, _union, _enum, signature);
//		}

//		Tag BinarySearch (string [] ctags_lines, TagKind kind, string name)
//		{
//			int low;
//			int high = ctags_lines.Length - 2; // last element is an empty string (because of the Split)
//			int mid;
//			int start_index = 0;

//			// Skip initial comment lines
//			while (ctags_lines [start_index].StartsWith ("!_"))
//				start_index++;

//			low = start_index;

//			while (low <= high) {
//				mid = (low + high) / 2;
//				string entry = ctags_lines [mid];
//				string tag_name = entry.Substring (0, entry.IndexOf ('\t'));
//				int res = string.CompareOrdinal (tag_name, name);

//				if (res < 0) {
//					low = mid + 1;
//				} else if (res > 0) {
//					high = mid - 1;
//				} else {
//					// The tag we are at has the same name than the one we are looking for
//					// but not necessarily the same type, the actual tag we are looking
//					// for might be higher up or down, so we try both, starting with going down.
//					int save = mid;
//					bool going_down = true;
//					bool eof = false;

//					while (true) {
//						Tag tag = ParseTag (entry);

//						if (tag == null)
//							return null;

//						if (tag.Kind == kind && tag_name == name)
//							return tag;

//						if (going_down) {
//							mid++;

//							if (mid >= ctags_lines.Length - 1)
//								eof = true;

//							if (!eof) {
//								entry = ctags_lines [mid];
//								tag_name = entry.Substring (0, entry.IndexOf ('\t'));

//								if (tag_name != name) {
//									going_down = false;
//									mid = save - 1;
//								}
//							} else {
//								going_down = false;
//								mid = save - 1;
//							}
//						} else { // going up
//							mid--;

//							if (mid < start_index)
//								return null;

//							entry = ctags_lines [mid];
//							tag_name = entry.Substring (0, entry.IndexOf ('\t'));

//							if (tag_name != name)
//								return null;
//						}
//					}
//				}
//			}

//			return null;
//		}

//		private struct SemiTag
//		{
//			readonly internal string name;
//			readonly internal TagKind kind;

//			internal SemiTag (string name, TagKind kind)
//			{
//				this.name = name;
//				this.kind = kind;
//			}

//			public override int GetHashCode ()
//			{
//				return (name + kind.ToString ()).GetHashCode ();
//			}
//		}

//		private const int cache_size = 10000;
//		private Dictionary<SemiTag, Tag> cache = new Dictionary<SemiTag, Tag> ();

//		public Tag FindTag (string name, TagKind kind, string ctags_output)
//		{
//			SemiTag semiTag = new SemiTag (name, kind);

//			if (cache.ContainsKey (semiTag))
//				return cache [semiTag];
//			else {
//				string [] ctags_lines = ctags_output.Split ('\n');
//				Tag tag = BinarySearch (ctags_lines, kind, name);
//				cache.Add (semiTag, tag);

//				return tag;
//			}
//		}

//		/// <summary>
//		/// Remove a file's parse information from the database.
//		/// </summary>
//		/// <param name="project">
//		/// A <see cref="Project"/>: The project to which the file belongs.
//		/// </param>
//		/// <param name="filename">
//		/// A <see cref="System.String"/>: The file.
//		/// </param>
//		public void RemoveFileInfo (Project project, string filename)
//		{
//			ProjectInformation info = ProjectInformationManager.Instance.Get (project);
//			lock (info) { info.RemoveFileInfo (filename); }
//			OnFileUpdated (new ClassPadEventArgs (project));
//		}

//		private static string [] diff (string [] a1, string [] a2)
//		{
//			List<string> res = new List<string> ();
//			List<string> right = new List<string> (a2);

//			foreach (string s in a1) {
//				if (!right.Contains (s))
//					res.Add (s);
//			}

//			return res.ToArray ();
//		}

//		/// <summary>
//		/// Wrapper method for the FileUpdated event.
//		/// </summary>
//		void OnFileUpdated (ClassPadEventArgs args)
//		{
//			if (null != FileUpdated) { FileUpdated (args); }
//		}

//		private class ProjectFilePair
//		{
//			string file;
//			Project project;

//			public ProjectFilePair (Project project, string file)
//			{
//				this.project = project;
//				this.file = file;
//			}

//			public string File {
//				get { return file; }
//			}

//			public Project Project {
//				get { return project; }
//			}

//			public override bool Equals (object other)
//			{
//				ProjectFilePair o = other as ProjectFilePair;

//				if (o == null)
//					return false;

//				if (file == o.File && project == o.Project)
//					return true;
//				else
//					return false;
//			}

//			public override int GetHashCode ()
//			{
//				return (project.ToString () + file).GetHashCode ();
//			}
//		}
//	}
//}