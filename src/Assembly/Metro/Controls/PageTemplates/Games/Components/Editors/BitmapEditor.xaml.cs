using Assembly.Helpers;
using Assembly.Metro.Dialogs;
using Assembly.Windows;
using Blamite.Blam;
using Blamite.Blam.Resources;
using Blamite.Blam.ThirdGen;
using Blamite.Injection;
using Blamite.IO;
using Blamite.Plugins;
using Blamite.Serialization;
using Blamite.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xml;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.Editors
{
	/// <summary>
	/// Interaction logic for BitmapEditor.xaml
	/// </summary>
	public partial class BitmapEditor
	{
		private readonly EngineDescription _buildInfo;
		private readonly TagEntry _tag;
		private readonly ICacheFile _cache;
		private readonly string _cacheLocation;
		private readonly IStreamManager _streamManager;
		private ResourceTable resourceTable;
		private List<byte[]> processedPages = new List<byte[]>();

		public BitmapEditor(EngineDescription buildInfo, string cacheLocation, TagEntry tag, ICacheFile cache, IStreamManager streamManager)
		{
			// #TODO: Notification changes
			InitializeComponent();

			_buildInfo = buildInfo;
			_cacheLocation = cacheLocation;
			_tag = tag;
			_cache = cache;
			_streamManager = streamManager;

			txtTagName.Text = _tag.TagFileName;
		}

		public void RefreshBitmapDisplay()
		{
			GetPages(_tag.RawTag);
		}

		public void GetPages(ITag bitmapTag)
		{
			// I don't feel like writing structure definitions, poaching extraction code
			// #TODO: Wrap all of this up under a neat function for other uses

			processedPages.Clear();

			string groupName = VariousFunctions.SterilizeTagGroupName(CharConstant.ToString(bitmapTag.Group.Magic)).Trim();
			string pluginPath = string.Format("{0}\\{1}\\{2}.xml", VariousFunctions.GetApplicationLocation() + @"Plugins", _buildInfo.Settings.GetSetting<string>("plugins"), groupName);

			if (!File.Exists(pluginPath) && _buildInfo.Settings.PathExists("fallbackPlugins"))
			{
				pluginPath = string.Format("{0}\\{1}\\{2}.xml", VariousFunctions.GetApplicationLocation() + @"Plugins", _buildInfo.Settings.GetSetting<string>("fallbackPlugins"), groupName);
			}

			if (pluginPath == null || !File.Exists(pluginPath))
			{
				StatusUpdater.Update("Plugin doesn't exist for 'bitmap', yet somehow you managed to get to the bitmap tab...");
				return;
			}

			DataBlockBuilder bitmapTagData;
			using (IReader reader = _streamManager.OpenRead())
			{
				bitmapTagData = new DataBlockBuilder(reader, bitmapTag, _cache, _buildInfo);
				using (XmlReader pluginReader = XmlReader.Create(pluginPath))
				{
					AssemblyPluginLoader.LoadPlugin(pluginReader, bitmapTagData);
				}
			}

			bool multiResouceMagic = false; // debugging 8k textures spread across space and time

			// Resource check
			if (bitmapTagData.ReferencedResources.Count == 0)
			{
				StatusUpdater.Update("Unable to find any resources in the current tag to get bitmap data from!");
				return;
			}
			else
			{
				if (bitmapTagData.ReferencedResources.Count > 1)
				{
					multiResouceMagic = true;
				}
				if (_cache.Resources != null) // mandrill compiled debugging cache
				{
					using (IReader reader = _streamManager.OpenRead())
					{
						resourceTable = _cache.Resources.LoadResourceTable(reader);
					}
				}
				else
				{
					StatusUpdater.Update("Unable to find any resources in the cache file at all! Failed to display bitmap.");
					return;
				}
			}

			// Grab all the raw pages required
			foreach (DatumIndex resourceDatum in bitmapTagData.ReferencedResources)
			{
				Resource currentResource = resourceTable.Resources[resourceDatum.Index];
				if (currentResource.Location == null)
				{
					StatusUpdater.Update("A bitmap resource had a null location, bad doo doo! Fix yo compiler nerd.");
					return;
				}

				foreach (ResourcePage currentPage in currentResource.Location.PagesToArray())
				{
					if (currentPage == null)
					{
						continue;
					}

					using (FileStream fileStream = File.OpenRead(_cacheLocation))
					{
						ThirdGenCacheFile resourceFile = (ThirdGenCacheFile)_cache;
						Stream resourceStream = fileStream;

						if (currentPage.FilePath != null) // Mandrill compiles everything into a single cache
						{
							ResourceCacheInfo resourceCacheInfo = App.AssemblyStorage.AssemblySettings.HalomapResourceCachePaths.FirstOrDefault(r => r.EngineName == _buildInfo.Name);
							string resourceCachePath = (resourceCacheInfo != null && resourceCacheInfo.ResourceCachePath != "") ? resourceCacheInfo.ResourceCachePath : Path.GetDirectoryName(_cacheLocation);
							resourceCachePath = Path.Combine(resourceCachePath ?? "", Path.GetFileName(currentPage.FilePath));

							if (!File.Exists(resourceCachePath))
							{
								StatusUpdater.Update("Bitmap exists outside of the local cache, was unable to find this cache: " + Path.GetFileName(resourceCachePath));
								return;
							}

							resourceStream = File.OpenRead(resourceCachePath);
							resourceFile = new ThirdGenCacheFile(new EndianReader(resourceStream, _cache.Endianness), _buildInfo, Path.GetFileName(_cacheLocation), _cache.BuildString);
						}

						byte[] pageData;
						ResourcePageExtractor pageExtractor = new ResourcePageExtractor(resourceFile);
						using (MemoryStream pageStream = new MemoryStream())
						{
							pageExtractor.ExtractPage(currentPage, resourceStream, pageStream);
							pageData = new byte[pageStream.Length];
							Buffer.BlockCopy(pageStream.GetBuffer(), 0, pageData, 0, (int)pageStream.Length);
						}

						processedPages.Add(pageData); // Store the page for use
					}
				}
			}
		}

		private void btnPreviewBitmap_Click(object sender, RoutedEventArgs e)
		{
			RefreshBitmapDisplay();
		}

		private void btnExtractBitmap_Click(object sender, RoutedEventArgs e)
		{

		}

		private void btnDumpRaw_Click(object sender, RoutedEventArgs e)
		{

		}
	}
}
