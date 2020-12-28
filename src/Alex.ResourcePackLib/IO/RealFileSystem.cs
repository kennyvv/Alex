using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Alex.ResourcePackLib.IO.Abstract;

namespace Alex.ResourcePackLib.IO
{
	public class RealFileSystem : IFilesystem
	{
		/// <inheritdoc />
		public IReadOnlyCollection<IFile> Entries { get; }

		private string Root { get; }
		public RealFileSystem(string path)
		{
			Root = path;
			
			List<IFile> entries = new List<IFile>();
			foreach (var file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
			{
				entries.Add(new FileSystemEntry(new FileInfo(file), Path.GetRelativePath(path, file)));
			}

			Entries = new ReadOnlyCollection<IFile>(entries);
		}

		/// <inheritdoc />
		public IFile GetEntry(string name)
		{
			return Entries.FirstOrDefault(x => x.FullName == name);
		}
		
		/// <inheritdoc />
		public void Dispose()
		{
			
		}

		public class FileSystemEntry : IFile
		{
			/// <inheritdoc />
			public string FullName { get; }

			/// <inheritdoc />
			public string Name => _fileInfo.Name;

			/// <inheritdoc />
			public long Length => _fileInfo.Length;

			private FileInfo _fileInfo;
			public FileSystemEntry(FileInfo fileInfo, string relativePath)
			{
				_fileInfo = fileInfo;
				FullName = relativePath;
			}

			/// <inheritdoc />
			public Stream Open()
			{
				return _fileInfo.Open(FileMode.Open);
			}
		}
	}
}