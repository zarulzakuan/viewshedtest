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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Rasters;
using Esri.ArcGISRuntime.Tasks;
using Esri.ArcGISRuntime.Tasks.Geoprocessing;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;
using System.Diagnostics;

namespace ViewshedTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        // Url for the geoprocessing service
        private const string _viewshedUrl =
            "https://sampleserver6.arcgisonline.com/arcgis/rest/services/Elevation/ESRI_Elevation_World/GPServer/Viewshed";

        // Used to store state of the geoprocessing task
        private bool _isExecutingGeoprocessing;

        // The graphics overlay to show where the user clicked in the map
        private GraphicsOverlay _inputOverlay;

        // The graphics overlay to display the result of the viewshed analysis
        private GraphicsOverlay _resultOverlay;
        Map myMap;
        public MainWindow()
        {
            InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            // Create a map with topographic basemap and an initial location
            myMap = new Map(BasemapType.Topographic, 3.46453080693902, 101.460480205504, 13);

            // Hook into the tapped event
            MyMapView.GeoViewTapped += OnMapViewTapped;

            // Create empty overlays for the user clicked location and the results of the viewshed analysis
            CreateOverlays();

            // Assign the map to the MapView
            MyMapView.Map = myMap;
        }

        private async void OnMapViewTapped(object sender, GeoViewInputEventArgs e)
        {
            // The geoprocessing task is still executing, don't do anything else (i.e. respond to
            // more user taps) until the processing is complete.
            if (_isExecutingGeoprocessing)
            {
                return;
            }

            // Indicate that the geoprocessing is running
            SetBusy();

            // Clear previous user click location and the viewshed geoprocessing task results
            _inputOverlay.Graphics.Clear();
            _resultOverlay.Graphics.Clear();

            // Get the tapped point
            MapPoint geometry = e.Location;

            // Create a marker graphic where the user clicked on the map and add it to the existing graphics overlay
            Graphic myInputGraphic = new Graphic(geometry);
            _inputOverlay.Graphics.Add(myInputGraphic);

            // Normalize the geometry if wrap-around is enabled
            //    This is necessary because of how wrapped-around map coordinates are handled by Runtime
            //    Without this step, the task may fail because wrapped-around coordinates are out of bounds.
            if (MyMapView.IsWrapAroundEnabled) { geometry = (MapPoint)GeometryEngine.NormalizeCentralMeridian(geometry); }

            try
            {
                // Execute the geoprocessing task using the user click location
                CalculateViewshed(geometry);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error");
            }
        }

        private async void CalculateViewshed(MapPoint location)
        {
            // Get the file name
            MapPoint Point = (MapPoint)GeometryEngine.Project(location, SpatialReferences.Wgs84);
            RunCmd(Point.X, Point.Y);
            string filepath = "C:\\Users\\zia13430\\dest.tif";

            // Load the raster file
            Raster myRasterFile = new Raster(filepath);

            // Create the layer
            RasterLayer myRasterLayer = new RasterLayer(myRasterFile);
            
            // Create a color map where values 0-149 are red and 150-249 are yellow.
            IEnumerable<Color> colors = new int[250]
               .Select((c, i) => i < 150 ? Color.Red : Color.Red);

            // Create a colormap renderer.
            ColormapRenderer colormapRenderer = new ColormapRenderer(colors);
            

            // Set the colormap renderer on the raster layer.
            myRasterLayer.Renderer = colormapRenderer;
            myRasterLayer.Opacity = 0.6;
            await MyMapView.SetViewpointAsync(new Viewpoint(Point, 75000), TimeSpan.FromSeconds(1));
            //myMap.InitialViewpoint = ;

            // Add the layer to the map
            myMap.OperationalLayers.Clear();
            myMap.OperationalLayers.Add(myRasterLayer);
            
            try
            {

            }
            catch (Exception ex)
            {

                MessageBox.Show("An error occurred. " + ex.ToString(), "Sample error");
            }
            finally
            {
                // Indicate that the geoprocessing is not running
                SetBusy(false);
            }
        }

        private void CreateOverlays()
        {
            // This function will create the overlays that show the user clicked location and the results of the
            // viewshed analysis. Note: the overlays will not be populated with any graphics at this point

            // Create renderer for input graphic. Set the size and color properties for the simple renderer
            SimpleRenderer myInputRenderer = new SimpleRenderer()
            {
                Symbol = new SimpleMarkerSymbol()
                {
                    Size = 15,
                    Color = Color.Red
                }
            };

            // Create overlay to where input graphic is shown
            _inputOverlay = new GraphicsOverlay()
            {
                Renderer = myInputRenderer
            };

            // Create fill renderer for output of the viewshed analysis. Set the color property of the simple renderer
            SimpleRenderer myResultRenderer = new SimpleRenderer()
            {
                Symbol = new SimpleFillSymbol()
                {
                    Color = Color.FromArgb(100, 226, 119, 40)
                }
            };

            // Create overlay to where viewshed analysis graphic is shown
            _resultOverlay = new GraphicsOverlay()
            {
                Renderer = myResultRenderer
            };

            // Add the created overlays to the MapView
            MyMapView.GraphicsOverlays.Add(_inputOverlay);
            MyMapView.GraphicsOverlays.Add(_resultOverlay);
        }

        private void SetBusy(bool isBusy = true)
        {
            // This function toggles the visibility of the 'BusyOverlay' Grid control defined in xaml,
            // sets the 'progress' control feedback status and updates the _isExecutingGeoprocessing
            // boolean to denote if the viewshed analysis is executing as a result of the user click
            // on the map

            if (isBusy)
            {
                // Change UI to indicate that the geoprocessing is running
                _isExecutingGeoprocessing = true;
                BusyOverlay.Visibility = Visibility.Visible;
                Progress.IsIndeterminate = true;
            }
            else
            {
                // Change UI to indicate that the geoprocessing is not running
                _isExecutingGeoprocessing = false;
                BusyOverlay.Visibility = Visibility.Collapsed;
                Progress.IsIndeterminate = false;
            }
        }

        public void RunCmd(double X, double Y)
        {
            String command = @" -ox "+ X + " -oy " + Y + " -md 10 -iv 255 -vv 0 -b 1 n03_e101_1arc_v3.tif dest.tif";
            Console.WriteLine(command);
            
            ProcessStartInfo cmdsi = new ProcessStartInfo("C:\\Users\\zia13430\\gdal\\bin\\gdal\\apps\\gdal_viewshed.exe");
            cmdsi.WindowStyle = ProcessWindowStyle.Hidden;
            cmdsi.CreateNoWindow = true;
            cmdsi.Arguments = command;
            cmdsi.UseShellExecute = false;
            cmdsi.WorkingDirectory = "C:\\Users\\zia13430";
            string oldpath = cmdsi.EnvironmentVariables["PATH"];

            Console.WriteLine(oldpath);

            Process cmd = Process.Start(cmdsi);

            cmd.WaitForExit();
        }
    }
}
