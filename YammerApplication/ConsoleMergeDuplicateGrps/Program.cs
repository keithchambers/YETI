// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;

namespace ConsoleMergeDuplicateGrps
{
    class Program
	{
		private static string Year = string.Empty;
		private static string YammerdirPath = ConfigurationManager.AppSettings["YammerdirPath"];
		private static string DedupPath = ConfigurationManager.AppSettings["DedupPath"];
		static void Main(string[] args)
		{
			MergeZipedGroups();
		}

		private static void MergeUnzipedGroups() {
			StreamWriter log;
			if (!File.Exists(".\\logfile.txt"))
			{
				log = new StreamWriter(".\\logfile.txt");
			}
			else
			{
				log = File.AppendText(".\\logfile.txt");
			}
			log.WriteLine("Time: " + DateTime.Now);
			log.WriteLine("Path: " + YammerdirPath);
			try
			{
				List<string> directoryList = Directory.EnumerateDirectories(YammerdirPath).ToList();
				List<string> dupDirectoryList = new List<string>();



				foreach (string direcTory in directoryList)
				{
					if (Directory.Exists(direcTory))
					{
						string grpName = string.Empty;
						grpName = direcTory.Substring(direcTory.LastIndexOf("\\") + 1);
						List<string> grFolderName = Directory.EnumerateDirectories(YammerdirPath, grpName.Substring(0, grpName.IndexOf(" ") + 1) + "*").ToList();
						if (grFolderName.Count() > 1)
						{
							dupDirectoryList.AddRange(grFolderName);
							//merge




							// Write to the file:
							foreach (string dic in grFolderName)
							{
								log.WriteLine("DuplicateGroupName: " + dic.Substring(direcTory.LastIndexOf("\\") + 1));
							}


							if (grFolderName[0].Length > grFolderName[1].Length)
							{
								FileSystem.CopyDirectory(grFolderName[1], grFolderName[0], false);
								
								Directory.Delete(grFolderName[1], true);
							}
							else
							{
								FileSystem.CopyDirectory(grFolderName[0], grFolderName[1], false);
								Directory.Delete(grFolderName[0], true);
							}



						}
					}
				}
			}
			catch (Exception ex)
			{

				log.WriteLine("Exception: " + ex.ToString());
				log.Close();

			}
			finally
			{
				log.Close();
			}
		}

		private static void ListFolderEndWithSpace()
		{
			StreamWriter log;
			if (!File.Exists(".\\groupsEndWithSpace.txt"))
			{
				log = new StreamWriter(".\\groupsEndWithSpace.txt");
			}
			else
			{
				log = File.AppendText(".\\groupsEndWithSpace.txt");
			}
			log.WriteLine("Time: " + DateTime.Now);
			log.WriteLine("Path: " + YammerdirPath);
			List<string> directoryList = Directory.EnumerateDirectories(YammerdirPath).ToList();
			foreach (string direcTory in directoryList)
			{
				if (Regex.IsMatch(direcTory, @"\s+$"))
				{
					log.WriteLine("EndWithSpace:'" + direcTory+"'");
				}
			}
			log.Close();
		}

		private static void MergeZipedGroups()
		{
			StreamWriter log;
			if (!File.Exists(".\\logfile.txt"))
			{
				log = new StreamWriter(".\\logfile.txt");
			}
			else
			{
				log = File.AppendText(".\\logfile.txt");
			}
			log.WriteLine("Time: " + DateTime.Now);
			log.WriteLine("Path: " + YammerdirPath);
			try
			{
				List<string> directoryList = Directory.EnumerateDirectories(YammerdirPath).ToList();
				List<string> dupDirectoryList = new List<string>();



				foreach (string direcTory in directoryList)
				{
					if (Directory.Exists(direcTory))
					{
						string grpName = string.Empty;
						grpName = direcTory.Substring(direcTory.LastIndexOf("\\") + 1);
						List<string> grFolderName = Directory.EnumerateDirectories(YammerdirPath, grpName.Substring(0, grpName.IndexOf(" ") + 1) + "*").ToList();
						if (grFolderName.Count() > 1)
						{
							dupDirectoryList.AddRange(grFolderName);

							// Write to the file:
							foreach (string dic in grFolderName)
							{
								log.WriteLine("DuplicateGroupName: '" + dic.Substring(direcTory.LastIndexOf("\\") + 1)+"'");
								//unzip
								ExtractFiles(dic);
							}

							string source=string.Empty;
							string target=string.Empty;

							if (grFolderName[0].Length > grFolderName[1].Length)
							{
								source = grFolderName[1];
								if (Regex.IsMatch(grFolderName[0], @"\s+$"))
								{
									target = Regex.Replace(grFolderName[0], @"\s+$", " _");
									//grFolderName[0].LastIndexOf(" ")
									Directory.Move(grFolderName[0], target);
                                    

									
								}
								else
								{
									target = grFolderName[0];
								}
							}
							else {
								source = grFolderName[0];
								if (Regex.IsMatch(grFolderName[1], @"\s+$"))
								{
									target = Regex.Replace(grFolderName[1], @"\s+$", " _");
									//grFolderName[0].LastIndexOf(" ")
									Directory.Move(grFolderName[1], target);

								

								}
								else
								{
									target = grFolderName[1];
								}
							}
							if (Directory.Exists(source))
							{
								CopyAll(new DirectoryInfo(source), new DirectoryInfo(target));
							
							
								Directory.Delete(source, true);
							}

							//zip
							string[] subfolders = Directory.GetDirectories(target);
							foreach (string subfolder in subfolders)
							{
								if (!File.Exists(subfolder + ".zip"))
								{
									ZipFile.CreateFromDirectory(subfolder, subfolder + ".zip");
								}
								Directory.Delete(subfolder, true);
							}
							string targetPath = Path.Combine(DedupPath, target.Substring(grFolderName[0].LastIndexOf("\\") + 1));
							if (!Directory.Exists(targetPath))
								Directory.CreateDirectory(targetPath);
							CopyAll(new DirectoryInfo(target), new DirectoryInfo(targetPath));


						}
					}
				}
			}
			catch (Exception ex)
			{

				log.WriteLine("Exception: " + ex.ToString());
				log.Close();

			}
			finally
			{
				log.Close();
			}
		}

		public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
		{
			if (source.FullName.ToLower() == target.FullName.ToLower())
			{
				return;
			}

			

			// Copy each file into it's new directory.
			foreach (FileInfo fi in source.GetFiles())
			{
				
				if (!File.Exists(Path.Combine(target.ToString(), fi.Name)))
				{
					Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
					fi.CopyTo(Path.Combine(target.ToString(), fi.Name), false);
				}
			}

			// Copy each subdirectory using recursion.
			foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
			{
				DirectoryInfo nextTargetSubDir =
					target.CreateSubdirectory(diSourceSubDir.Name);
				CopyAll(diSourceSubDir, nextTargetSubDir);
			}
		}
		private static void ExtractFiles(string duplicateGroup)
		{
			DirectoryInfo ydi = new DirectoryInfo(duplicateGroup);

			FileInfo[] fileList = ydi.GetFiles("*.zip");
			string status = string.Empty;
			if (fileList.Count() > 0)
			{
				foreach (FileInfo yfi in fileList)
				{
					string zipPath = yfi.FullName;
					try
					{
						ZipFile.ExtractToDirectory(yfi.FullName.ToString(), yfi.FullName.ToString().Replace(".zip", string.Empty));
						yfi.Delete();

					}
					catch (Exception ex)
					{
						StreamWriter log;
						if (!File.Exists(".\\logfile.txt"))
						{
							log = new StreamWriter(".\\logfile.txt");
						}
						else
						{
							log = File.AppendText(".\\logfile.txt");
						}
						log.WriteLine("Exception: " + ex.ToString());
						log.Close();
					}
					

				}
			}
		}
	}
}
