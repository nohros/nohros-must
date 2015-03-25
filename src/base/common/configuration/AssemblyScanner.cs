﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Nohros.Logging;

namespace Nohros.Configuration
{
  /// <summary>
  /// Helpers for assembly scanning operations.
  /// </summary>
  public class AssemblyScanner
  {
    static string[] kDefaultAssemblyExclusions = {};

    readonly string base_directory_to_scan_;
    readonly internal List<string> assemblies_to_skip_;

    /// <summary>
    /// Creates a new scanner that will scan the base directory of the current
    /// appdomain
    /// </summary>
    public AssemblyScanner()
      : this(AppDomain.CurrentDomain.BaseDirectory) {
    }

    /// <summary>
    /// Creates a scanner for the given directory
    /// </summary>
    /// <param name="base_directory_to_scan"></param>
    public AssemblyScanner(string base_directory_to_scan) {
      base_directory_to_scan_ = base_directory_to_scan;
      ThrowExceptions = true;
      ScanNestedDirectories = true;
      IncludeExesInScan = true;
      IncludeAppDomainAssemblies = false;
      TypesToSkip = new List<Type>();
      assemblies_to_skip_ = new List<string>();
    }

    /// <summary>
    /// Traverses the specified base directory including all sub-directories,
    /// generating a list of assemblies that can be scanned, a list of
    /// skipped files, and a list of errors that occurred while scanning.
    /// Scanned files may be skipped when they're not a .NET assembly.
    /// </summary>
    public AssemblyScannerResults GetScannableAssemblies() {
      var results = new AssemblyScannerResults();

      if (IncludeAppDomainAssemblies) {
        List<Assembly> matching_app_domain_assemblies =
          MatchingAssembliesFromAppDomain();

        foreach (var assembly in matching_app_domain_assemblies) {
          ScanAssembly(AssemblyPath(assembly), results);
        }
      }

      IEnumerable<FileInfo> assembly_files = ScanDirectoryForAssemblyFiles();
      foreach (var assembly_file in assembly_files) {
        ScanAssembly(assembly_file.FullName, results);
      }

      results.RemoveDuplicates();

      return results;
    }

    List<Assembly> MatchingAssembliesFromAppDomain() {
      return AppDomain
        .CurrentDomain
        .GetAssemblies()
        .Where(
          assembly =>
            !assembly.IsDynamic &&
              IsIncluded(assembly.GetName().Name))
        .ToList();
    }

    static string AssemblyPath(Assembly assembly) {
      var uri = new UriBuilder(assembly.CodeBase);
      return Uri.UnescapeDataString(uri.Path).Replace('/', '\\');
    }

    void ScanAssembly(string assembly_path, AssemblyScannerResults results) {
      Assembly assembly;

      if (!IsIncluded(Path.GetFileNameWithoutExtension(assembly_path))) {
        var skipped_file = new SkippedFile(assembly_path,
          "File was explicitly excluded from scanning.");
        results.SkippedFiles.Add(skipped_file);
        return;
      }

      var compilation_mode = Image.GetCompilationMode(assembly_path);
      if (compilation_mode == Image.CompilationMode.NativeOrInvalid) {
        var skipped_file = new SkippedFile(assembly_path,
          "File is not a .NET assembly.");
        results.SkippedFiles.Add(skipped_file);
        return;
      }

      if (!Environment.Is64BitProcess &&
        compilation_mode == Image.CompilationMode.CLRx64) {
        var skipped_file = new SkippedFile(assembly_path,
          "x64 .NET assembly can't be loaded by a 32Bit process.");
        results.SkippedFiles.Add(skipped_file);
        return;
      }

      try {
        if (IsRuntimeAssembly(assembly_path)) {
          var skipped_file = new SkippedFile(assembly_path,
            "Assembly .net runtime assembly.");
          results.SkippedFiles.Add(skipped_file);
          return;
        }

        assembly = Assembly.LoadFrom(assembly_path);

        if (results.Assemblies.Contains(assembly)) {
          return;
        }
      } catch (BadImageFormatException ex) {
        assembly = null;
        results.ErrorsThrownDuringScanning = true;

        if (ThrowExceptions) {
          var error_message =
            String.Format(
              "Could not load '{0}'. Consider excluding that assembly from the scanning.",
              assembly_path);
          throw new Exception(error_message, ex);
        }
      }

      if (assembly == null) {
        return;
      }

      try {
        //will throw if assembly cannot be loaded
        results.Types.AddRange(assembly.GetTypes().Where(IsAllowedType));
      } catch (ReflectionTypeLoadException e) {
        results.ErrorsThrownDuringScanning = true;

        var error_message = FormatReflectionTypeLoadException(assembly_path, e);
        if (ThrowExceptions) {
          throw new Exception(error_message);
        }

        MustLogger.ForCurrentProcess.Warn(error_message);
        results.Types.AddRange(e.Types.Where(IsAllowedType));
      }

      results.Assemblies.Add(assembly);
    }

    internal static bool IsRuntimeAssembly(string assembly_path) {
      var assembly_name = AssemblyName.GetAssemblyName(assembly_path);
      return IsRuntimeAssembly(assembly_name);
    }

    static bool IsRuntimeAssembly(AssemblyName assembly_name) {
      var public_key_token = assembly_name.GetPublicKeyToken();
      var lower_invariant =
        BitConverter
          .ToString(public_key_token)
          .Replace("-", String.Empty)
          .ToLowerInvariant();

      //System
      if (lower_invariant == "b77a5c561934e089") {
        return true;
      }

      //Web
      if (lower_invariant == "b03f5f7f11d50a3a") {
        return true;
      }

      //patterns and practices
      if (lower_invariant == "31bf3856ad364e35") {
        return true;
      }

      return false;
    }

    static string FormatReflectionTypeLoadException(string file_name,
      ReflectionTypeLoadException e) {
      var sb = new StringBuilder();

      sb.AppendLine(string.Format("Could not enumerate all types for '{0}'.",
        file_name));

      if (!e.LoaderExceptions.Any()) {
        sb.AppendLine(string.Format("Exception message: {0}", e));
        return sb.ToString();
      }

      var files = new List<string>();
      var sb_file_load_exception = new StringBuilder();
      var sb_generic_exception = new StringBuilder();

      foreach (var ex in e.LoaderExceptions) {
        var load_exception = ex as FileLoadException;

        if (load_exception != null) {
          if (!files.Contains(load_exception.FileName)) {
            files.Add(load_exception.FileName);
            sb_file_load_exception.AppendLine(load_exception.FileName);
          }
          continue;
        }

        sb_generic_exception.AppendLine(ex.ToString());
      }

      if (sb_generic_exception.ToString().Length > 0) {
        sb.AppendLine("Exceptions:");
        sb.AppendLine(sb_generic_exception.ToString());
      }

      if (sb_file_load_exception.ToString().Length > 0) {
        sb.AppendLine(
          "It looks like you may be missing binding redirects in your config file for the following assemblies:");
        sb.Append(sb_file_load_exception);
        sb.AppendLine(
          "For more information see http://msdn.microsoft.com/en-us/library/7wd6ex19(v=vs.100).aspx");
      }

      return sb.ToString();
    }

    IEnumerable<FileInfo> ScanDirectoryForAssemblyFiles() {
      var base_dir = new DirectoryInfo(base_directory_to_scan_);
      var search_option = ScanNestedDirectories
        ? SearchOption.AllDirectories
        : SearchOption.TopDirectoryOnly;
      return
        GetFileSearchPatternsToUse()
          .SelectMany(ext => base_dir.GetFiles(ext, search_option));
    }

    IEnumerable<string> GetFileSearchPatternsToUse() {
      yield return "*.dll";

      if (IncludeExesInScan) {
        yield return "*.exe";
      }
    }

    bool IsIncluded(string assembly_or_file_name) {
      var is_explicitly_excluded =
        assemblies_to_skip_
          .Any(excluded => IsMatch(excluded, assembly_or_file_name));
      if (is_explicitly_excluded) {
        return false;
      }

      var is_excluded_by_default =
        kDefaultAssemblyExclusions
          .Any(exclusion => IsMatch(exclusion, assembly_or_file_name));
      if (is_excluded_by_default) {
        return false;
      }

      return true;
    }

    static bool IsMatch(string expression1, string expression2) {
      return
        DistillLowerAssemblyName(expression1) ==
          DistillLowerAssemblyName(expression2);
    }

    bool IsAllowedType(Type type) {
      return type != null &&
        !type.IsValueType &&
        !(type.GetCustomAttributes(typeof (CompilerGeneratedAttribute), false)
              .Length > 0) &&
        !TypesToSkip.Contains(type);
    }

    static string DistillLowerAssemblyName(string assembly_or_file_name) {
      var lower_assembly_name = assembly_or_file_name.ToLowerInvariant();
      if (lower_assembly_name.EndsWith(".dll")) {
        lower_assembly_name = lower_assembly_name.Substring(0,
          lower_assembly_name.Length - 4);
      }
      return lower_assembly_name;
    }

    internal List<Type> TypesToSkip { get; set; }

    internal bool IncludeAppDomainAssemblies { get; set; }
    internal bool IncludeExesInScan { get; set; }
    public bool ScanNestedDirectories { get; set; }

    /// <summary>
    /// Determines if the scanner should throw exceptions or not
    /// </summary>
    public bool ThrowExceptions { get; set; }
  }
}
