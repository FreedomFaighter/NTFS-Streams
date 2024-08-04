/*
  * Trinet.Core.IO.Ntfs - Utilities for working with alternate data streams on NTFS file systems.
  * Copyright (C) 2002-2016 Richard Deeming
  * 
  * This code is free software: you can redistribute it and/or modify it under the terms of either
  * - the Code Project Open License (CPOL) version 1 or later; or
  * - the GNU General Public License as published by the Free Software Foundation, version 3 or later; or
  * - the BSD 2-Clause License;
  * 
  * This code is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
  * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
  * See the license files for details.
  * 
  * You should have received a copy of the licenses along with this code. 
  * If not, see <http://www.codeproject.com/info/cpol10.aspx>, <http://www.gnu.org/licenses/> 
  * and <http://opensource.org/licenses/bsd-license.php>.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Permissions;

namespace Trinet.Core.IO.Ntfs
{
	/// <summary>
	/// File-system utilities.
	/// </summary>
	public static class FileSystem
	{
		#region List Streams

		/// <summary>
		/// <span style="font-weight:bold;color:#a00;">(Extension Method)</span><br />
		/// Returns a read-only list of alternate data streams for the specified file.
		/// </summary>
		/// <param name="file">
		/// The <see cref="FileSystemInfo"/> to inspect.
		/// </param>
		/// <returns>
		/// A read-only list of <see cref="AlternateDataStreamInfo"/> objects
		/// representing the alternate data streams for the specified file, if any.
		/// If no streams are found, returns an empty list.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="file"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="FileNotFoundException">
		/// The specified <paramref name="file"/> does not exist.
		/// </exception>
		/// <exception cref="SecurityException">
		/// The caller does not have the required permission. 
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		/// The caller does not have the required permission.
		/// </exception>
		public static IList<AlternateDataStreamInfo> ListAlternateDataStreams(this FileSystemInfo file)
		{
			if (null == file) throw new ArgumentNullException(nameof(file));
			if (!file.Exists) throw new FileNotFoundException($"File not found in {System.Reflection.MethodInfo.GetCurrentMethod().Name} method.", file.FullName);

			string path = file.FullName;

#if NET35
			new FileIOPermission(FileIOPermissionAccess.Read, path).Demand();
#endif

			return SafeNativeMethods.ListStreams(path)
				.Select(s => new AlternateDataStreamInfo(path, s))
				.ToList().AsReadOnly();
		}

		/// <summary>
		/// Returns a read-only list of alternate data streams for the specified file.
		/// </summary>
		/// <param name="filePath">
		/// The full path of the file to inspect.
		/// </param>
		/// <returns>
		/// A read-only list of <see cref="AlternateDataStreamInfo"/> objects
		/// representing the alternate data streams for the specified file, if any.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="filePath"/> is <see langword="null"/> or an empty string.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// <paramref name="filePath"/> is not a valid file path.
		/// </exception>
		/// <exception cref="FileNotFoundException">
		/// The specified <paramref name="filePath"/> does not exist.
		/// </exception>
		/// <exception cref="SecurityException">
		/// The caller does not have the required permission. 
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		/// The caller does not have the required permission.
		/// </exception>
		public static IList<AlternateDataStreamInfo> ListAlternateDataStreams(string filePath)
		{
			if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
			if (!SafeNativeMethods.FileExists(filePath)) throw new FileNotFoundException($"File not found in {System.Reflection.MethodInfo.GetCurrentMethod().Name} method.", filePath);

#if NET35
			new FileIOPermission(FileIOPermissionAccess.Read, filePath).Demand();
#endif

			return SafeNativeMethods.ListStreams(filePath)
				.Select(s => new AlternateDataStreamInfo(filePath, s))
				.ToList().AsReadOnly();
		}

		#endregion

		#region Stream Exists

		/// <summary>
		/// <span style="font-weight:bold;color:#a00;">(Extension Method)</span><br />
		/// Returns a flag indicating whether the specified alternate data stream exists.
		/// </summary>
		/// <param name="file">
		/// The <see cref="FileInfo"/> to inspect.
		/// </param>
		/// <param name="streamName">
		/// The name of the stream to find.
		/// </param>
		/// <returns>
		/// <see langword="true"/> if the specified stream exists;
		/// otherwise, <see langword="false"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="file"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// <paramref name="streamName"/> contains invalid characters.
		/// </exception>
		public static bool AlternateDataStreamExists(this FileSystemInfo file, string streamName)
		{
			if (null == file) throw new ArgumentNullException(nameof(file));
			SafeNativeMethods.ValidateStreamName(streamName);

			string path = SafeNativeMethods.BuildStreamPath(file.FullName, streamName);
			return SafeNativeMethods.FileExists(path);
		}

		/// <summary>
		/// Returns a flag indicating whether the specified alternate data stream exists.
		/// </summary>
		/// <param name="filePath">
		/// The path of the file to inspect.
		/// </param>
		/// <param name="streamName">
		/// The name of the stream to find.
		/// </param>
		/// <returns>
		/// <see langword="true"/> if the specified stream exists;
		/// otherwise, <see langword="false"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="filePath"/> is <see langword="null"/> or an empty string.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// <para><paramref name="filePath"/> is not a valid file path.</para>
		/// <para>-or-</para>
		/// <para><paramref name="streamName"/> contains invalid characters.</para>
		/// </exception>
		public static bool AlternateDataStreamExists(string filePath, string streamName)
		{
			if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
			SafeNativeMethods.ValidateStreamName(streamName);

			string path = SafeNativeMethods.BuildStreamPath(filePath, streamName);
			return SafeNativeMethods.FileExists(path);
		}

		#endregion

		#region Open Stream

		/// <summary>
		/// <span style="font-weight:bold;color:#a00;">(Extension Method)</span><br />
		/// Opens an alternate data stream.
		/// </summary>
		/// <param name="file">
		/// The <see cref="FileInfo"/> which contains the stream.
		/// </param>
		/// <param name="streamName">
		/// The name of the stream to open.
		/// </param>
		/// <param name="mode">
		/// One of the <see cref="FileMode"/> values, indicating how the stream is to be opened.
		/// </param>
		/// <returns>
		/// An <see cref="AlternateDataStreamInfo"/> representing the stream.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="file"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="FileNotFoundException">
		/// The specified <paramref name="file"/> was not found.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// <paramref name="streamName"/> contains invalid characters.
		/// </exception>
		/// <exception cref="NotSupportedException">
		/// <paramref name="mode"/> is either <see cref="FileMode.Truncate"/> or <see cref="FileMode.Append"/>.
		/// </exception>
		/// <exception cref="IOException">
		/// <para><paramref name="mode"/> is <see cref="FileMode.Open"/>, and the stream doesn't exist.</para>
		/// <para>-or-</para>
		/// <para><paramref name="mode"/> is <see cref="FileMode.CreateNew"/>, and the stream already exists.</para>
		/// </exception>
		/// <exception cref="SecurityException">
		/// The caller does not have the required permission. 
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		/// The caller does not have the required permission, or the file is read-only.
		/// </exception>
		public static AlternateDataStreamInfo GetAlternateDataStream(this FileSystemInfo file, string streamName, FileMode mode)
		{
			if (null == file) throw new ArgumentNullException(nameof(file));
			if (!file.Exists) throw new FileNotFoundException($"File not found in {System.Reflection.MethodInfo.GetCurrentMethod().Name} method.", file.FullName);
			SafeNativeMethods.ValidateStreamName(streamName);

			if (FileMode.Truncate == mode || FileMode.Append == mode)
			{
				throw new NotSupportedException(Resources.Error_InvalidMode(mode));
			}

#if NET35
			FileIOPermissionAccess permAccess = (FileMode.Open == mode) ? FileIOPermissionAccess.Read : FileIOPermissionAccess.Read | FileIOPermissionAccess.Write;
			new FileIOPermission(permAccess, file.FullName).Demand();
#endif

			string path = SafeNativeMethods.BuildStreamPath(file.FullName, streamName);
			bool exists = SafeNativeMethods.FileExists(path);

			if (!exists && FileMode.Open == mode)
			{
				throw new IOException(Resources.Error_StreamNotFound(streamName, file.Name));
			}
			if (exists && FileMode.CreateNew == mode)
			{
				throw new IOException(Resources.Error_StreamExists(streamName, file.Name));
			}

			return new AlternateDataStreamInfo(file.FullName, streamName, path, exists);
		}

		/// <summary>
		/// <span style="font-weight:bold;color:#a00;">(Extension Method)</span><br />
		/// Opens an alternate data stream.
		/// </summary>
		/// <param name="file">
		/// The <see cref="FileInfo"/> which contains the stream.
		/// </param>
		/// <param name="streamName">
		/// The name of the stream to open.
		/// </param>
		/// <returns>
		/// An <see cref="AlternateDataStreamInfo"/> representing the stream.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="file"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="FileNotFoundException">
		/// The specified <paramref name="file"/> was not found.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// <paramref name="streamName"/> contains invalid characters.
		/// </exception>
		/// <exception cref="SecurityException">
		/// The caller does not have the required permission. 
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		/// The caller does not have the required permission, or the file is read-only.
		/// </exception>
		public static AlternateDataStreamInfo GetAlternateDataStream(this FileSystemInfo file, string streamName)
		{
			return file.GetAlternateDataStream(streamName, FileMode.OpenOrCreate);
		}

		/// <summary>
		/// Opens an alternate data stream.
		/// </summary>
		/// <param name="filePath">
		/// The path of the file which contains the stream.
		/// </param>
		/// <param name="streamName">
		/// The name of the stream to open.
		/// </param>
		/// <param name="mode">
		/// One of the <see cref="FileMode"/> values, indicating how the stream is to be opened.
		/// </param>
		/// <returns>
		/// An <see cref="AlternateDataStreamInfo"/> representing the stream.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="filePath"/> is <see langword="null"/> or an empty string.
		/// </exception>
		/// <exception cref="FileNotFoundException">
		/// The specified <paramref name="filePath"/> was not found.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// <para><paramref name="filePath"/> is not a valid file path.</para>
		/// <para>-or-</para>
		/// <para><paramref name="streamName"/> contains invalid characters.</para>
		/// </exception>
		/// <exception cref="NotSupportedException">
		/// <paramref name="mode"/> is either <see cref="FileMode.Truncate"/> or <see cref="FileMode.Append"/>.
		/// </exception>
		/// <exception cref="IOException">
		/// <para><paramref name="mode"/> is <see cref="FileMode.Open"/>, and the stream doesn't exist.</para>
		/// <para>-or-</para>
		/// <para><paramref name="mode"/> is <see cref="FileMode.CreateNew"/>, and the stream already exists.</para>
		/// </exception>
		/// <exception cref="SecurityException">
		/// The caller does not have the required permission. 
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		/// The caller does not have the required permission, or the file is read-only.
		/// </exception>
		public static AlternateDataStreamInfo GetAlternateDataStream(string filePath, string streamName, FileMode mode)
		{
			if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
			if (!SafeNativeMethods.FileExists(filePath)) throw new FileNotFoundException($"File not found in {System.Reflection.MethodInfo.GetCurrentMethod().Name} method.", filePath);
			SafeNativeMethods.ValidateStreamName(streamName);

			if (FileMode.Truncate == mode || FileMode.Append == mode)
			{
				throw new NotSupportedException(Resources.Error_InvalidMode(mode));
			}

#if NET35
			FileIOPermissionAccess permAccess = (FileMode.Open == mode) ? FileIOPermissionAccess.Read : FileIOPermissionAccess.Read | FileIOPermissionAccess.Write;
			new FileIOPermission(permAccess, filePath).Demand();
#endif

			string path = SafeNativeMethods.BuildStreamPath(filePath, streamName);
			bool exists = SafeNativeMethods.FileExists(path);

			if (!exists && FileMode.Open == mode)
			{
				throw new IOException(Resources.Error_StreamNotFound(streamName, filePath));
			}
			if (exists && FileMode.CreateNew == mode)
			{
				throw new IOException(Resources.Error_StreamExists(streamName, filePath));
			}

			return new AlternateDataStreamInfo(filePath, streamName, path, exists);
		}

		/// <summary>
		/// Opens an alternate data stream.
		/// </summary>
		/// <param name="filePath">
		/// The path of the file which contains the stream.
		/// </param>
		/// <param name="streamName">
		/// The name of the stream to open.
		/// </param>
		/// <returns>
		/// An <see cref="AlternateDataStreamInfo"/> representing the stream.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="filePath"/> is <see langword="null"/> or an empty string.
		/// </exception>
		/// <exception cref="FileNotFoundException">
		/// The specified <paramref name="filePath"/> was not found.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// <para><paramref name="filePath"/> is not a valid file path.</para>
		/// <para>-or-</para>
		/// <para><paramref name="streamName"/> contains invalid characters.</para>
		/// </exception>
		/// <exception cref="SecurityException">
		/// The caller does not have the required permission. 
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		/// The caller does not have the required permission, or the file is read-only.
		/// </exception>
		public static AlternateDataStreamInfo GetAlternateDataStream(string filePath, string streamName)
		{
			return GetAlternateDataStream(filePath, streamName, FileMode.OpenOrCreate);
		}

		#endregion

		#region Delete Stream

		/// <summary>
		/// <span style="font-weight:bold;color:#a00;">(Extension Method)</span><br />
		/// Deletes the specified alternate data stream if it exists.
		/// </summary>
		/// <param name="file">
		/// The <see cref="FileInfo"/> to inspect.
		/// </param>
		/// <param name="streamName">
		/// The name of the stream to delete.
		/// </param>
		/// <returns>
		/// <see langword="true"/> if the specified stream is deleted;
		/// otherwise, <see langword="false"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="file"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// <paramref name="streamName"/> contains invalid characters.
		/// </exception>
		/// <exception cref="SecurityException">
		/// The caller does not have the required permission. 
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		/// The caller does not have the required permission, or the file is read-only.
		/// </exception>
		/// <exception cref="IOException">
		/// The specified file is in use. 
		/// </exception>
		public static bool DeleteAlternateDataStream(this FileSystemInfo file, string streamName)
		{
			if (null == file) throw new ArgumentNullException(nameof(file));
			SafeNativeMethods.ValidateStreamName(streamName);

#if NET35
			const FileIOPermissionAccess permAccess = FileIOPermissionAccess.Write;
			new FileIOPermission(permAccess, file.FullName).Demand();
#endif

			var result = false;
			if (file.Exists)
			{
				string path = SafeNativeMethods.BuildStreamPath(file.FullName, streamName);
				if (SafeNativeMethods.FileExists(path))
				{
					result = SafeNativeMethods.SafeDeleteFile(path);
				}
			}

			return result;
		}

		/// <summary>
		/// Deletes the specified alternate data stream if it exists.
		/// </summary>
		/// <param name="filePath">
		/// The path of the file to inspect.
		/// </param>
		/// <param name="streamName">
		/// The name of the stream to find.
		/// </param>
		/// <returns>
		/// <see langword="true"/> if the specified stream is deleted;
		/// otherwise, <see langword="false"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="filePath"/> is <see langword="null"/> or an empty string.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// <para><paramref name="filePath"/> is not a valid file path.</para>
		/// <para>-or-</para>
		/// <para><paramref name="streamName"/> contains invalid characters.</para>
		/// </exception>
		/// <exception cref="SecurityException">
		/// The caller does not have the required permission. 
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		/// The caller does not have the required permission, or the file is read-only.
		/// </exception>
		/// <exception cref="IOException">
		/// The specified file is in use. 
		/// </exception>
		public static bool DeleteAlternateDataStream(string filePath, string streamName)
		{
			if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
			SafeNativeMethods.ValidateStreamName(streamName);

#if NET35
			const FileIOPermissionAccess permAccess = FileIOPermissionAccess.Write;
			new FileIOPermission(permAccess, filePath).Demand();
#endif

			var result = false;
			if (SafeNativeMethods.FileExists(filePath))
			{
				string path = SafeNativeMethods.BuildStreamPath(filePath, streamName);
				if (SafeNativeMethods.FileExists(path))
				{
					result = SafeNativeMethods.SafeDeleteFile(path);
				}
			}

			return result;
		}

		#endregion
	}
}
