using Blamite.Blam;
using Blamite.IO;
using Blamite.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Shapes;

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

		public BitmapEditor(EngineDescription buildInfo, string cacheLocation, TagEntry tag, ICacheFile cache, IStreamManager streamManager)
		{
			InitializeComponent();

			// Scope
			_buildInfo = buildInfo;
			_cacheLocation = cacheLocation;
			_tag = tag;
			_cache = cache;
			_streamManager = streamManager;

			// Populate
			RefreshBitmapDisplay();
		}

		public void RefreshBitmapDisplay()
		{
			txtTagName.Text = _tag.TagFileName;
		}
	}
}
