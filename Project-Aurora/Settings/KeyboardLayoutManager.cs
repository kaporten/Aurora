﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using Aurora.Devices;
using System.Drawing;
using System.Windows.Media.Imaging;
using Aurora.Settings.Keycaps;

namespace Aurora.Settings
{
    public class KeyboardKey
    {
        public String visualName;
        public Devices.DeviceKeys tag;
        public bool line_break;
        public double margin_left;
        public double margin_top;
        public double width;
        public double height;
        public double font_size;
        public int width_bits;
        public int height_bits;
        public int margin_left_bits;
        public int margin_top_bits;
        public bool enabled = true;
        public bool absolute_location = false;
        public String image = "";

        public KeyboardKey(String text, Devices.DeviceKeys tag, bool enabled = true, bool linebreak = false, double fontsize = 12, double margin_left = 7, double margin_top = 0, double width = 30, double height = 30, int width_bits = 2, int height_bits = 2, int margin_left_bits = 0, int margin_top_bits = 0)
        {
            this.visualName = text;
            this.tag = tag;
            this.line_break = linebreak;
            this.width = width;
            this.height = height;
            this.font_size = fontsize;
            this.margin_left = margin_left;
            this.margin_top = margin_top;
            this.width_bits = width_bits;
            this.height_bits = height_bits;
            this.margin_left_bits = margin_left_bits;
            this.margin_top_bits = margin_top_bits;
            this.enabled = enabled;
        }
    }

    public enum KeyboardRegion
    {
        TopLeft = 1,
        TopRight = 2,
        BottomLeft = 3,
        BottomRight = 4
    }

    public class VirtualGroupConfiguration
    {
        public bool replace_RWin_with_FN = false;

        public Devices.DeviceKeys[] keys_to_remove = new Devices.DeviceKeys[] { };

        public Devices.DeviceKeys[] keys_to_set_as_new_line = new Devices.DeviceKeys[] { };

        /// <summary>
        /// A list of paths for each included group json
        /// </summary>
        public string[] included_features = new string[] { };
    }

    public class VirtualGroup
    {
        public string group_tag;

        public KeyboardRegion origin_region;

        public List<KeyboardKey> grouped_keys = new List<KeyboardKey>();

        private RectangleF _region = new RectangleF(0, 0, 0, 0);

        public RectangleF Region { get { return _region; } }

        private Rectangle _region_bitmap = new Rectangle(0, 0, 0, 0);

        public Rectangle BitmapRegion { get { return _region_bitmap; } }

        public VirtualGroup()
        {

        }

        public VirtualGroup(KeyboardKey[] keys)
        {
            double layout_height = 0;
            double layout_width = 0;
            double current_height = 0;
            double current_width = 0;

            int width_bit = 0;
            int height_bit = 0;
            int width_bit_max = 1;
            int height_bit_max = 1;

            foreach (var key in keys)
            {
                grouped_keys.Add(key);

                if (key.width + key.margin_left > 0)
                    current_width += key.width + key.margin_left;

                if (key.margin_top > 0)
                    current_height += key.margin_top;


                if (layout_width < current_width)
                    layout_width = current_width;

                if (key.line_break)
                {
                    current_height += 37;
                    current_width = 0;
                }

                if (layout_height < current_height)
                    layout_height = current_height;


                int key_tly = key.margin_top_bits + height_bit;
                int key_tlx = key.margin_left_bits + width_bit;

                int key_bry = key_tly + key.height_bits;
                int key_brx = key_tlx + key.width_bits;

                if (width_bit_max < key_brx) width_bit_max = key_brx;
                if (height_bit_max < key_bry) height_bit_max = key_bry;


                if (key.line_break)
                {
                    height_bit += 3;
                    width_bit = 0;
                }
                else
                {
                    width_bit = key_brx;
                    height_bit = key_tly;
                }

            }

            _region.Width = (float)layout_width;
            _region.Height = (float)layout_height;

            _region_bitmap.Width = width_bit_max;
            _region_bitmap.Height = height_bit_max;

            //NormalizeKeys();
        }

        public void AddFeature(KeyboardKey[] keys, KeyboardRegion insertion_region = KeyboardRegion.TopLeft)
        {
            double location_x = 0.0D;
            double location_y = 0.0D;
            int location_x_bit = 0;
            int location_y_bit = 0;

            if (insertion_region == KeyboardRegion.TopRight)
            {
                location_x = _region.Width;
                location_x_bit = _region_bitmap.Width;
            }
            else if (insertion_region == KeyboardRegion.BottomLeft)
            {
                location_y = _region.Height;
                location_y_bit = _region_bitmap.Height;
            }
            else if (insertion_region == KeyboardRegion.BottomRight)
            {
                location_x = _region.Width;
                location_y = _region.Height;
                location_x_bit = _region_bitmap.Width;
                location_y_bit = _region_bitmap.Height;
            }

            float added_width = 0.0f;
            float added_height = 0.0f;
            int added_width_bits = 0;
            int added_height_bits = 0;

            foreach (var key in keys)
            {
                key.margin_left += location_x;
                key.margin_top += location_y;

                key.margin_left_bits += location_x_bit;
                key.margin_top_bits += location_y_bit;

                grouped_keys.Add(key);

                if (key.width + key.margin_left > _region.Width)
                    _region.Width += (float)(key.width + key.margin_left - location_x);
                else if (key.margin_left + added_width < 0)
                {
                    added_width = -(float)(key.margin_left);
                    _region.Width -= (float)(key.margin_left);
                }

                if (key.height + key.margin_top > _region.Height)
                    _region.Height += (float)(key.height + key.margin_top - location_y);
                else if (key.margin_top + added_height < 0)
                {
                    added_height = -(float)(key.margin_top);
                    _region.Height -= (float)(key.margin_top);
                }


                if (key.width_bits + key.margin_left_bits > _region_bitmap.Width)
                    _region_bitmap.Width += key.width_bits + key.margin_left_bits - location_x_bit;
                else if (key.margin_left_bits + added_width_bits < 0)
                {
                    added_width_bits = -key.margin_left_bits;
                    _region_bitmap.Width -= key.margin_left_bits;
                }

                if (key.height_bits + key.margin_top_bits > _region_bitmap.Height)
                    _region_bitmap.Height += key.height_bits + key.margin_top_bits - location_y_bit;
                else if (key.margin_top_bits + added_height_bits < 0)
                {
                    added_height_bits = -key.margin_top_bits;
                    _region_bitmap.Height -= key.margin_top_bits;
                }

            }

            NormalizeKeys();
        }

        private void NormalizeKeys()
        {
            double x_correction = 0.0D;
            double y_correction = 0.0D;

            int x_correction_bit = 0;
            int y_correction_bit = 0;

            foreach (var key in grouped_keys)
            {
                if (!key.absolute_location)
                    continue;

                if (key.margin_left < x_correction)
                    x_correction = key.margin_left;

                if (key.margin_top < y_correction)
                    y_correction = key.margin_top;

                if (key.margin_left_bits < x_correction_bit)
                    x_correction_bit = key.margin_left_bits;

                if (key.margin_top_bits < y_correction_bit)
                    y_correction_bit = key.margin_top_bits;
            }

            if (grouped_keys.Count > 0)
            {
                grouped_keys[0].margin_top -= y_correction;
                grouped_keys[0].margin_top_bits -= y_correction_bit;

                bool previous_linebreak = true;
                foreach (var key in grouped_keys)
                {
                    if (key.absolute_location)
                    {
                        key.margin_top -= y_correction;
                        key.margin_left -= x_correction;
                        key.margin_top_bits -= y_correction_bit;
                        key.margin_left_bits -= x_correction_bit;
                    }
                    else
                    {
                        if (previous_linebreak && !key.line_break)
                        {
                            key.margin_left -= x_correction;
                            key.margin_left_bits -= x_correction_bit;
                        }

                        previous_linebreak = key.line_break;
                    }
                }

            }
        }

        public void Clear()
        {
            _region = new RectangleF(0, 0, 0, 0);
            _region_bitmap = new Rectangle(0, 0, 0, 0);
            grouped_keys.Clear();
        }

        internal void AdjustFNKey()
        {
            var applicable_keys = grouped_keys.FindAll(key => key.tag == DeviceKeys.RIGHT_WINDOWS);

            foreach (var key in applicable_keys)
            {
                key.tag = DeviceKeys.FN_Key;
                key.visualName = "FN";
            }
        }

        internal void RemoveKeys(DeviceKeys[] keys_to_remove)
        {
            var applicable_keys = grouped_keys.RemoveAll(key => keys_to_remove.Contains(key.tag));

            double layout_height = 0;
            double layout_width = 0;
            double current_height = 0;
            double current_width = 0;

            int width_bit = 0;
            int height_bit = 0;
            int width_bit_max = 1;
            int height_bit_max = 1;

            foreach (var key in grouped_keys)
            {
                if (key.width + key.margin_left > 0)
                    current_width += key.width + key.margin_left;

                if (key.margin_top > 0)
                    current_height += key.margin_top;


                if (layout_width < current_width)
                    layout_width = current_width;

                if (key.line_break)
                {
                    current_height += 37;
                    current_width = 0;
                }

                if (layout_height < current_height)
                    layout_height = current_height;


                int key_tly = key.margin_top_bits + height_bit;
                int key_tlx = key.margin_left_bits + width_bit;

                int key_bry = key_tly + key.height_bits;
                int key_brx = key_tlx + key.width_bits;

                if (width_bit_max < key_brx) width_bit_max = key_brx;
                if (height_bit_max < key_bry) height_bit_max = key_bry;


                if (key.line_break)
                {
                    height_bit += 3;
                    width_bit = 0;
                }
                else
                {
                    width_bit = key_brx;
                    height_bit = key_tly;
                }

            }

            _region.Width = (float)layout_width;
            _region.Height = (float)layout_height;

            _region_bitmap.Width = width_bit_max;
            _region_bitmap.Height = height_bit_max;

        }

        internal void SetNewLineKeys(DeviceKeys[] keys_to_set_as_new_line)
        {
            var applicable_keys = grouped_keys.FindAll(key => keys_to_set_as_new_line.Contains(key.tag));

            foreach (var key in applicable_keys)
                key.line_break = true;
        }
    }

    public class KeyboardLayoutManager
    {
        private VirtualGroup virtual_keyboard_group;

        //private List<KeyboardKey> keyboard = new List<KeyboardKey>();

        private Dictionary<Devices.DeviceKeys, IKeycap> _virtual_keyboard_map = new Dictionary<DeviceKeys, IKeycap>();

        private Grid _virtual_keyboard = new Grid();

        public Grid Virtual_keyboard
        {
            get
            {
                return _virtual_keyboard;
            }
        }

        private FrameworkElement last_selected_element;

        private double bitmap_one_pixel = 12.0; // 12 pixels = 1 byte

        private Dictionary<Devices.DeviceKeys, BitmapRectangle> bitmap_map = new Dictionary<Devices.DeviceKeys, BitmapRectangle>();

        public delegate void LayoutUpdatedEventHandler(object sender);

        public event LayoutUpdatedEventHandler KeyboardLayoutUpdated;

        private String cultures_folder = "kb_layouts";

        private PreferredKeyboardLocalization _loaded_localization = PreferredKeyboardLocalization.None;

        public PreferredKeyboardLocalization Loaded_Localization
        {
            get
            {
                return _loaded_localization;
            }
        }

        private String layoutsPath = "";

        public KeyboardLayoutManager()
        {
            layoutsPath = Path.Combine(Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName), cultures_folder);
        }

        public void LoadBrand(PreferredKeyboard keyboard_preference = PreferredKeyboard.None, PreferredMouse mouse_preference = PreferredMouse.None, MouseOrientationType mouse_orientation = MouseOrientationType.RightHanded)
        {
            try
            {
                //System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

                //Global.logger.LogLine("Loading brand: " + brand.ToString() + " for: " + System.Threading.Thread.CurrentThread.CurrentCulture.Name);

                //Load keyboard layout
                if (Directory.Exists(layoutsPath))
                {
                    string culture = System.Threading.Thread.CurrentThread.CurrentCulture.Name;

                    switch (Global.Configuration.keyboard_localization)
                    {
                        case PreferredKeyboardLocalization.None:
                            break;
                        case PreferredKeyboardLocalization.intl:
                            culture = "intl";
                            break;
                        case PreferredKeyboardLocalization.us:
                            culture = "en-US";
                            break;
                        case PreferredKeyboardLocalization.uk:
                            culture = "en-GB";
                            break;
                        case PreferredKeyboardLocalization.ru:
                            culture = "ru-RU";
                            break;
                        case PreferredKeyboardLocalization.fr:
                            culture = "fr-FR";
                            break;
                        case PreferredKeyboardLocalization.de:
                            culture = "de-DE";
                            break;
                        case PreferredKeyboardLocalization.jpn:
                            culture = "ja-JP";
                            break;
                        case PreferredKeyboardLocalization.nordic:
                            culture = "nordic";
                            break;
                    }

                    switch (culture)
                    {
                        case ("ja-JP"):
                            LoadCulture("jpn");
                            break;
                        case ("de-DE"):
                        case ("hsb-DE"):
                        case ("dsb-DE"):
                            _loaded_localization = PreferredKeyboardLocalization.de;
                            LoadCulture("de");
                            break;
                        case ("fr-FR"):
                        case ("br-FR"):
                        case ("oc-FR"):
                        case ("co-FR"):
                        case ("gsw-FR"):
                            _loaded_localization = PreferredKeyboardLocalization.fr;
                            LoadCulture("fr");
                            break;
                        case ("cy-GB"):
                        case ("gd-GB"):
                        case ("en-GB"):
                            _loaded_localization = PreferredKeyboardLocalization.uk;
                            LoadCulture("uk");
                            break;
                        case ("ru-RU"):
                        case ("tt-RU"):
                        case ("ba-RU"):
                        case ("sah-RU"):
                            _loaded_localization = PreferredKeyboardLocalization.ru;
                            LoadCulture("ru");
                            break;
                        case ("en-US"):
                            _loaded_localization = PreferredKeyboardLocalization.us;
                            LoadCulture("us");
                            break;
                        case ("da-DK"):
                        case ("se-SE"):
                        case ("nb-NO"):
                        case ("nn-NO"):
                        case ("nordic"):
                            _loaded_localization = PreferredKeyboardLocalization.nordic;
                            LoadCulture("nordic");
                            break;
                        default:
                            _loaded_localization = PreferredKeyboardLocalization.intl;
                            LoadCulture("intl");
                            break;
                    }
                }

                var layoutConfigPath = "";

                if (keyboard_preference == PreferredKeyboard.Logitech_G910)
                    layoutConfigPath = Path.Combine(layoutsPath, "logitech_g910.json");
                else if (keyboard_preference == PreferredKeyboard.Logitech_G810)
                    layoutConfigPath = Path.Combine(layoutsPath, "logitech_g810.json");
                else if (keyboard_preference == PreferredKeyboard.Logitech_G410)
                    layoutConfigPath = Path.Combine(layoutsPath, "logitech_g410.json");
                else if (keyboard_preference == PreferredKeyboard.Corsair_K95)
                    layoutConfigPath = Path.Combine(layoutsPath, "corsair_k95.json");
                else if (keyboard_preference == PreferredKeyboard.Corsair_K70)
                    layoutConfigPath = Path.Combine(layoutsPath, "corsair_k70.json");
                else if (keyboard_preference == PreferredKeyboard.Corsair_K65)
                    layoutConfigPath = Path.Combine(layoutsPath, "corsair_k65.json");
                else if (keyboard_preference == PreferredKeyboard.Corsair_STRAFE)
                    layoutConfigPath = Path.Combine(layoutsPath, "corsair_strafe.json");
                else if (keyboard_preference == PreferredKeyboard.Razer_Blackwidow)
                    layoutConfigPath = Path.Combine(layoutsPath, "razer_blackwidow.json");
                else if (keyboard_preference == PreferredKeyboard.Razer_Blackwidow_X)
                    layoutConfigPath = Path.Combine(layoutsPath, "razer_blackwidow_x.json");
                else if (keyboard_preference == PreferredKeyboard.Razer_Blackwidow_TE)
                    layoutConfigPath = Path.Combine(layoutsPath, "razer_blackwidow_te.json");
                else if (keyboard_preference == PreferredKeyboard.Masterkeys_Pro_L)
                    layoutConfigPath = Path.Combine(layoutsPath, "masterkeys_pro_l.json");
                else if (keyboard_preference == PreferredKeyboard.Masterkeys_Pro_S)
                    layoutConfigPath = Path.Combine(layoutsPath, "masterkeys_pro_s.json");
                //else if (keyboard_preference == PreferredKeyboard.Roccat_Ryos)
                //    layoutConfigPath = Path.Combine(layoutsPath, "roccat_ryos.json");
                else
                {
                    LoadNone();
                    return;
                }

                if (!String.IsNullOrWhiteSpace(layoutConfigPath) && File.Exists(layoutConfigPath))
                {
                    string content = File.ReadAllText(layoutConfigPath, Encoding.UTF8);
                    VirtualGroupConfiguration layoutConfig = JsonConvert.DeserializeObject<VirtualGroupConfiguration>(content, new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });

                    if (layoutConfig.replace_RWin_with_FN)
                        virtual_keyboard_group.AdjustFNKey();

                    virtual_keyboard_group.SetNewLineKeys(layoutConfig.keys_to_set_as_new_line);
                    virtual_keyboard_group.RemoveKeys(layoutConfig.keys_to_remove);

                    foreach (string feature in layoutConfig.included_features)
                    {
                        string feature_path = Path.Combine(layoutsPath, "Extra Features", feature);

                        if (File.Exists(feature_path))
                        {
                            string feature_content = File.ReadAllText(feature_path, Encoding.UTF8);
                            VirtualGroup feature_config = JsonConvert.DeserializeObject<VirtualGroup>(feature_content, new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });

                            virtual_keyboard_group.AddFeature(feature_config.grouped_keys.ToArray(), feature_config.origin_region);
                        }
                    }

                    string mouse_feature_path = "";

                    switch (mouse_preference)
                    {
                        case PreferredMouse.Logitech_G900:
                            mouse_feature_path = Path.Combine(layoutsPath, "Extra Features", "logitech_g900_features.json");
                            break;
                        case PreferredMouse.Corsair_Sabre:
                            mouse_feature_path = Path.Combine(layoutsPath, "Extra Features", "corsair_sabre_features.json");
                            break;
                        case PreferredMouse.Corsair_M65:
                            mouse_feature_path = Path.Combine(layoutsPath, "Extra Features", "corsair_m65_features.json");
                            break;
                        case PreferredMouse.Corsair_Katar:
                            mouse_feature_path = Path.Combine(layoutsPath, "Extra Features", "corsair_katar_features.json");
                            break;
                        case PreferredMouse.Clevo_Touchpad:
                            mouse_feature_path = Path.Combine(layoutsPath, "Extra Features", "clevo_touchpad_features.json");
                            break;
                    }

                    if (!string.IsNullOrWhiteSpace(mouse_feature_path))
                    {
                        string feature_content = File.ReadAllText(mouse_feature_path, Encoding.UTF8);
                        VirtualGroup feature_config = JsonConvert.DeserializeObject<VirtualGroup>(feature_content, new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });

                        if (mouse_orientation == MouseOrientationType.LeftHanded)
                        {
                            if (feature_config.origin_region == KeyboardRegion.TopRight)
                                feature_config.origin_region = KeyboardRegion.TopLeft;
                            else if (feature_config.origin_region == KeyboardRegion.BottomRight)
                                feature_config.origin_region = KeyboardRegion.BottomLeft;

                            double outline_width = 0.0;
                            int outline_width_bits = 0;

                            foreach (var key in feature_config.grouped_keys)
                            {
                                if (outline_width == 0.0 && outline_width_bits == 0) //We found outline (NOTE: Outline has to be first in the grouped keys)
                                {
                                    if (key.tag == DeviceKeys.NONE)
                                    {
                                        outline_width = key.width + 2 * key.margin_left;
                                        outline_width_bits = key.width_bits + 2 * key.margin_left_bits;
                                    }
                                }

                                key.margin_left -= outline_width;
                                key.margin_left_bits -= outline_width_bits;
                            }

                        }

                        virtual_keyboard_group.AddFeature(feature_config.grouped_keys.ToArray(), feature_config.origin_region);
                    }

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            //Perform end of load functions
            CalculateBitmap();
            CreateUserControl();

            KeyboardLayoutUpdated?.Invoke(this);
        }

        private int PixelToByte(int pixel)
        {
            return PixelToByte((double)pixel);
        }

        private int PixelToByte(double pixel)
        {
            return (int)Math.Round(pixel / (double)(this.bitmap_one_pixel));
        }

        private void CalculateBitmap()
        {
            int width_bit = 0;
            int height_bit = 0;
            int width_bit_max = 1;
            int height_bit_max = 1;
            bitmap_map.Clear();

            foreach (KeyboardKey key in virtual_keyboard_group.grouped_keys)
            {
                int key_tly = key.margin_top_bits + height_bit;
                int key_tlx = key.margin_left_bits + width_bit;

                int key_bry = key_tly + key.height_bits;
                int key_brx = key_tlx + key.width_bits;

                if (key.absolute_location)
                    this.bitmap_map[key.tag] = new BitmapRectangle(key.margin_left_bits, key.margin_top_bits, key_brx - key_tlx, key_bry - key_tly);
                else
                    this.bitmap_map[key.tag] = new BitmapRectangle(key_tlx, key_tly, key_brx - key_tlx, key_bry - key_tly);

                if (!key.absolute_location)
                {
                    if (width_bit_max < key_brx) width_bit_max = key_brx;
                    if (height_bit_max < key_bry) height_bit_max = key_bry;


                    if (key.line_break)
                    {
                        height_bit += PixelToByte(37);
                        width_bit = 0;
                    }
                    else
                    {
                        width_bit = key_brx;
                        height_bit = key_tly;
                    }
                }
            }

            Global.effengine.SetCanvasSize(virtual_keyboard_group.BitmapRegion.Width, virtual_keyboard_group.BitmapRegion.Height);
            Global.effengine.SetBitmapping(this.bitmap_map);
        }

        private void virtualkeyboard_key_selected(FrameworkElement key)
        {
            if (key.Tag is Devices.DeviceKeys && (Devices.DeviceKeys)key.Tag != DeviceKeys.NONE)
            {
                //Multi key
                if (Global.key_recorder.IsSingleKey())
                {
                    Global.key_recorder.AddKey((Devices.DeviceKeys)(key.Tag));
                    Global.key_recorder.StopRecording();
                }
                else
                {
                    if (Global.key_recorder.HasRecorded((Devices.DeviceKeys)(key.Tag)))
                        Global.key_recorder.RemoveKey((Devices.DeviceKeys)(key.Tag));
                    else
                        Global.key_recorder.AddKey((Devices.DeviceKeys)(key.Tag));
                    last_selected_element = key;
                }
            }
        }

        private void keyboard_grid_pressed(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border && (sender as Border).Child != null && (sender as Border).Child is TextBlock)
            {
                virtualkeyboard_key_selected((sender as Border).Child as TextBlock);
            }
            else if (sender is Border && (sender as Border).Tag != null)
            {
                virtualkeyboard_key_selected(sender as Border);
            }
        }

        private void keyboard_grid_moved(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (sender is Border && (sender as Border).Child != null && (sender as Border).Child is TextBlock && last_selected_element != ((sender as Border).Child as TextBlock))
                {
                    virtualkeyboard_key_selected((sender as Border).Child as TextBlock);
                }
                else if (sender is Border && (sender as Border).Tag != null && last_selected_element != (sender as Border))
                {
                    virtualkeyboard_key_selected(sender as Border);
                }
            }
        }

        private void CreateUserControl()
        {
            Grid new_virtual_keyboard = new Grid();
            _virtual_keyboard_map.Clear();

            double layout_height = 0;
            double layout_width = 0;

            double baseline_x = 0.0;
            double baseline_y = 0.0;
            double current_height = 0;
            double current_width = 0;

            string images_path = Path.Combine(layoutsPath, "Extra Features", "images");

            foreach (KeyboardKey key in virtual_keyboard_group.grouped_keys)
            {
                double keyMargin_Left = key.margin_left;
                double keyMargin_Top = key.margin_top;

                string image_path = "";

                if (!String.IsNullOrWhiteSpace(key.image))
                    image_path = Path.Combine(images_path, key.image);

                UserControl keycap;

                switch (Global.Configuration.virtualkeyboard_keycap_type)
                {
                    case KeycapType.Default_backglow:
                        keycap = new Control_DefaultKeycapBackglow(key, image_path);
                        break;
                    case KeycapType.Default_backglow_only:
                        keycap = new Control_DefaultKeycapBackglowOnly(key, image_path);
                        break;
                    case KeycapType.Colorized:
                        keycap = new Control_ColorizedKeycap(key, image_path);
                        break;
                    case KeycapType.Colorized_blank:
                        keycap = new Control_ColorizedKeycapBlank(key, image_path);
                        break;
                    default:
                        keycap = new Control_DefaultKeycap(key, image_path);
                        break;
                }

                new_virtual_keyboard.Children.Add(keycap);

                if (key.tag != DeviceKeys.NONE && !_virtual_keyboard_map.ContainsKey(key.tag) && keycap is IKeycap)
                    _virtual_keyboard_map.Add(key.tag, keycap as IKeycap);

                if (key.absolute_location)
                    keycap.Margin = new Thickness(key.margin_left, key.margin_top, 0, 0);
                else
                    keycap.Margin = new Thickness(current_width + key.margin_left, current_height + key.margin_top, 0, 0);

                if (key.tag == DeviceKeys.ESC)
                {
                    baseline_x = keycap.Margin.Left;
                    baseline_y = keycap.Margin.Top;
                }

                if (!key.absolute_location)
                {
                    if (key.width + keyMargin_Left > 0)
                        current_width += key.width + keyMargin_Left;

                    if (keyMargin_Top > 0)
                        current_height += keyMargin_Top;


                    if (layout_width < current_width)
                        layout_width = current_width;

                    if (key.line_break)
                    {
                        current_height += 37;
                        current_width = 0;
                        //isFirstInRow = true;
                    }

                    if (layout_height < current_height)
                        layout_height = current_height;
                }
            }

            if (virtual_keyboard_group.grouped_keys.Count == 0)
            {
                //No items, display error
                Label error_message = new Label();

                DockPanel info_panel = new DockPanel();

                TextBlock info_message = new TextBlock()
                {
                    Text = "No keyboard selected\r\nPlease select your keyboard in the settings",
                    TextAlignment = TextAlignment.Center,
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 0, 0)),
                };

                DockPanel.SetDock(info_message, Dock.Top);
                info_panel.Children.Add(info_message);

                DockPanel info_instruction = new DockPanel();

                info_instruction.Children.Add(new TextBlock()
                {
                    Text = "Press (",
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 0, 0)),
                    VerticalAlignment = VerticalAlignment.Center
                });

                info_instruction.Children.Add(new System.Windows.Controls.Image()
                {
                    Source = new BitmapImage(new Uri(@"Resources/settings_icon.png", UriKind.Relative)),
                    Stretch = Stretch.Uniform,
                    Height = 40.0,
                    VerticalAlignment = VerticalAlignment.Center
                });

                info_instruction.Children.Add(new TextBlock()
                {
                    Text = ") and go into \"Devices & Wrappers\" tab",
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 0, 0)),
                    VerticalAlignment = VerticalAlignment.Center
                });

                DockPanel.SetDock(info_instruction, Dock.Bottom);
                info_panel.Children.Add(info_instruction);

                error_message.Content = info_panel;

                error_message.FontSize = 16.0;
                error_message.FontWeight = FontWeights.Bold;
                error_message.HorizontalContentAlignment = HorizontalAlignment.Center;
                error_message.VerticalContentAlignment = VerticalAlignment.Center;

                new_virtual_keyboard.Children.Add(error_message);

                //Update size
                new_virtual_keyboard.Width = 850;
                new_virtual_keyboard.Height = 200;
            }
            else
            {
                //Update size
                new_virtual_keyboard.Width = virtual_keyboard_group.Region.Width;
                new_virtual_keyboard.Height = virtual_keyboard_group.Region.Height;
            }

            _virtual_keyboard.Children.Clear();
            _virtual_keyboard = new_virtual_keyboard;

            Global.logger.LogLine("Baseline X = " + (float)baseline_x, Logging_Level.Info, false);
            Global.logger.LogLine("Baseline Y = " + (float)baseline_y, Logging_Level.Info, false);
            Effects.grid_baseline_x = (float)baseline_x;
            Effects.grid_baseline_y = (float)baseline_y;
            Effects.grid_height = (float)new_virtual_keyboard.Height;
            Effects.grid_width = (float)new_virtual_keyboard.Width;
        }

        private void LoadCulture(String culture)
        {
            var fileName = "Plain Keyboard\\layout." + culture + ".json";
            var layoutPath = Path.Combine(layoutsPath, fileName);

            if (!File.Exists(layoutPath))
                LoadDefault();

            string content = File.ReadAllText(layoutPath, Encoding.UTF8);
            KeyboardKey[] keyboard = JsonConvert.DeserializeObject<KeyboardKey[]>(content, new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });

            virtual_keyboard_group = new VirtualGroup(keyboard);

            /*
            if (keyboard.Count > 0)
                keyboard.Last().line_break = false;

            keyboard.Add(new KeyboardKey("Mouse/\r\nHeadset", Devices.DeviceKeys.Peripheral, true, true, 12, 45, -60, 90, 90, 6, 6, 4, -3));

            if (keyboard.Count > 0)
                keyboard.Last().line_break = true;
            */
        }

        public void LoadNone()
        {
            virtual_keyboard_group.Clear();

            CalculateBitmap();
            CreateUserControl();

            KeyboardLayoutUpdated?.Invoke(this);
        }

        public void LoadDefault()
        {
            List<KeyboardKey> keyboard = new List<KeyboardKey>();

            keyboard.Add(new KeyboardKey("ESC", Devices.DeviceKeys.ESC));

            keyboard.Add(new KeyboardKey("F1", Devices.DeviceKeys.F1, true, false, 12, 32));
            keyboard.Add(new KeyboardKey("F2", Devices.DeviceKeys.F2));
            keyboard.Add(new KeyboardKey("F3", Devices.DeviceKeys.F3));
            keyboard.Add(new KeyboardKey("F4", Devices.DeviceKeys.F4));

            keyboard.Add(new KeyboardKey("F5", Devices.DeviceKeys.F5, true, false, 12, 34));
            keyboard.Add(new KeyboardKey("F6", Devices.DeviceKeys.F6));
            keyboard.Add(new KeyboardKey("F7", Devices.DeviceKeys.F7));
            keyboard.Add(new KeyboardKey("F8", Devices.DeviceKeys.F8));

            keyboard.Add(new KeyboardKey("F9", Devices.DeviceKeys.F9, true, false, 12, 29));
            keyboard.Add(new KeyboardKey("F10", Devices.DeviceKeys.F10));
            keyboard.Add(new KeyboardKey("F11", Devices.DeviceKeys.F11));
            keyboard.Add(new KeyboardKey("F12", Devices.DeviceKeys.F12));

            keyboard.Add(new KeyboardKey("PRINT", Devices.DeviceKeys.PRINT_SCREEN, true, false, 9, 14));
            keyboard.Add(new KeyboardKey("SCRL\r\nLOCK", Devices.DeviceKeys.SCROLL_LOCK, true, false, 9));
            keyboard.Add(new KeyboardKey("PAUSE", Devices.DeviceKeys.PAUSE_BREAK, true, true, 9));

            keyboard.Add(new KeyboardKey("~", Devices.DeviceKeys.TILDE));
            keyboard.Add(new KeyboardKey("1", Devices.DeviceKeys.ONE));
            keyboard.Add(new KeyboardKey("2", Devices.DeviceKeys.TWO));
            keyboard.Add(new KeyboardKey("3", Devices.DeviceKeys.THREE));
            keyboard.Add(new KeyboardKey("4", Devices.DeviceKeys.FOUR));
            keyboard.Add(new KeyboardKey("5", Devices.DeviceKeys.FIVE));
            keyboard.Add(new KeyboardKey("6", Devices.DeviceKeys.SIX));
            keyboard.Add(new KeyboardKey("7", Devices.DeviceKeys.SEVEN));
            keyboard.Add(new KeyboardKey("8", Devices.DeviceKeys.EIGHT));
            keyboard.Add(new KeyboardKey("9", Devices.DeviceKeys.NINE));
            keyboard.Add(new KeyboardKey("0", Devices.DeviceKeys.ZERO));
            keyboard.Add(new KeyboardKey("-", Devices.DeviceKeys.MINUS));
            keyboard.Add(new KeyboardKey("=", Devices.DeviceKeys.EQUALS));
            keyboard.Add(new KeyboardKey("BACKSPACE", Devices.DeviceKeys.BACKSPACE, true, false, 12, 7, 0, 67));

            keyboard.Add(new KeyboardKey("INSERT", Devices.DeviceKeys.INSERT, true, false, 9, 14));
            keyboard.Add(new KeyboardKey("HOME", Devices.DeviceKeys.HOME, true, false, 9));
            keyboard.Add(new KeyboardKey("PAGE\r\nUP", Devices.DeviceKeys.HOME, true, false, 9));

            keyboard.Add(new KeyboardKey("NUM\r\nLOCK", Devices.DeviceKeys.NUM_LOCK, true, false, 9, 14));
            keyboard.Add(new KeyboardKey("/", Devices.DeviceKeys.NUM_SLASH));
            keyboard.Add(new KeyboardKey("*", Devices.DeviceKeys.NUM_ASTERISK));
            keyboard.Add(new KeyboardKey("-", Devices.DeviceKeys.NUM_MINUS, true, true));

            keyboard.Add(new KeyboardKey("TAB", Devices.DeviceKeys.TAB, true, false, 12, 7, 0, 50));
            keyboard.Add(new KeyboardKey("Q", Devices.DeviceKeys.Q));
            keyboard.Add(new KeyboardKey("W", Devices.DeviceKeys.W));
            keyboard.Add(new KeyboardKey("E", Devices.DeviceKeys.E));
            keyboard.Add(new KeyboardKey("R", Devices.DeviceKeys.R));
            keyboard.Add(new KeyboardKey("T", Devices.DeviceKeys.T));
            keyboard.Add(new KeyboardKey("Y", Devices.DeviceKeys.Y));
            keyboard.Add(new KeyboardKey("U", Devices.DeviceKeys.U));
            keyboard.Add(new KeyboardKey("I", Devices.DeviceKeys.I));
            keyboard.Add(new KeyboardKey("O", Devices.DeviceKeys.O));
            keyboard.Add(new KeyboardKey("P", Devices.DeviceKeys.P));
            keyboard.Add(new KeyboardKey("{", Devices.DeviceKeys.OPEN_BRACKET));
            keyboard.Add(new KeyboardKey("}", Devices.DeviceKeys.CLOSE_BRACKET));
            keyboard.Add(new KeyboardKey("\\", Devices.DeviceKeys.BACKSLASH, true, false, 12, 7, 0, 49));

            keyboard.Add(new KeyboardKey("DEL", Devices.DeviceKeys.DELETE, true, false, 9, 12));
            keyboard.Add(new KeyboardKey("END", Devices.DeviceKeys.END, true, false, 9));
            keyboard.Add(new KeyboardKey("PAGE\r\nDOWN", Devices.DeviceKeys.PAGE_DOWN, true, false, 9));

            keyboard.Add(new KeyboardKey("7", Devices.DeviceKeys.NUM_SEVEN, true, false, 12, 14));
            keyboard.Add(new KeyboardKey("8", Devices.DeviceKeys.NUM_EIGHT));
            keyboard.Add(new KeyboardKey("9", Devices.DeviceKeys.NUM_NINE));
            keyboard.Add(new KeyboardKey("+", Devices.DeviceKeys.NUM_PLUS, true, true, 12, 7, 0, 30, 69));

            keyboard.Add(new KeyboardKey("CAPS\r\nLOCK", Devices.DeviceKeys.CAPS_LOCK, true, false, 9, 7, 0, 60));
            keyboard.Add(new KeyboardKey("A", Devices.DeviceKeys.A));
            keyboard.Add(new KeyboardKey("S", Devices.DeviceKeys.S));
            keyboard.Add(new KeyboardKey("D", Devices.DeviceKeys.D));
            keyboard.Add(new KeyboardKey("F", Devices.DeviceKeys.F));
            keyboard.Add(new KeyboardKey("G", Devices.DeviceKeys.G));
            keyboard.Add(new KeyboardKey("H", Devices.DeviceKeys.H));
            keyboard.Add(new KeyboardKey("J", Devices.DeviceKeys.J));
            keyboard.Add(new KeyboardKey("K", Devices.DeviceKeys.K));
            keyboard.Add(new KeyboardKey("L", Devices.DeviceKeys.L));
            keyboard.Add(new KeyboardKey(":", Devices.DeviceKeys.SEMICOLON));
            keyboard.Add(new KeyboardKey("\"", Devices.DeviceKeys.APOSTROPHE));
            keyboard.Add(new KeyboardKey("ENTER", Devices.DeviceKeys.ENTER, true, false, 12, 7, 0, 76));

            keyboard.Add(new KeyboardKey("4", Devices.DeviceKeys.NUM_FOUR, true, false, 12, 130));
            keyboard.Add(new KeyboardKey("5", Devices.DeviceKeys.NUM_FIVE));
            keyboard.Add(new KeyboardKey("6", Devices.DeviceKeys.NUM_SIX, true, true));
            //Space taken up by +

            keyboard.Add(new KeyboardKey("SHIFT", Devices.DeviceKeys.LEFT_SHIFT, true, false, 12, 7, 0, 78));
            keyboard.Add(new KeyboardKey("Z", Devices.DeviceKeys.Z));
            keyboard.Add(new KeyboardKey("X", Devices.DeviceKeys.X));
            keyboard.Add(new KeyboardKey("C", Devices.DeviceKeys.C));
            keyboard.Add(new KeyboardKey("V", Devices.DeviceKeys.V));
            keyboard.Add(new KeyboardKey("B", Devices.DeviceKeys.B));
            keyboard.Add(new KeyboardKey("N", Devices.DeviceKeys.N));
            keyboard.Add(new KeyboardKey("M", Devices.DeviceKeys.M));
            keyboard.Add(new KeyboardKey("<", Devices.DeviceKeys.COMMA));
            keyboard.Add(new KeyboardKey(">", Devices.DeviceKeys.PERIOD));
            keyboard.Add(new KeyboardKey("?", Devices.DeviceKeys.FORWARD_SLASH));
            keyboard.Add(new KeyboardKey("SHIFT", Devices.DeviceKeys.RIGHT_SHIFT, true, false, 12, 7, 0, 95));

            keyboard.Add(new KeyboardKey("UP", Devices.DeviceKeys.ARROW_UP, true, false, 9, 49));

            keyboard.Add(new KeyboardKey("1", Devices.DeviceKeys.NUM_ONE, true, false, 12, 51));
            keyboard.Add(new KeyboardKey("2", Devices.DeviceKeys.NUM_TWO));
            keyboard.Add(new KeyboardKey("3", Devices.DeviceKeys.NUM_THREE));
            keyboard.Add(new KeyboardKey("ENTER", Devices.DeviceKeys.NUM_ENTER, true, true, 9, 7, 0, 30, 67));

            keyboard.Add(new KeyboardKey("CTRL", Devices.DeviceKeys.RIGHT_CONTROL, true, false, 12, 7, 0, 51));
            keyboard.Add(new KeyboardKey("WIN", Devices.DeviceKeys.RIGHT_WINDOWS, true, false, 12, 5, 0, 39));
            keyboard.Add(new KeyboardKey("ALT", Devices.DeviceKeys.RIGHT_ALT, true, false, 12, 5, 0, 42));

            keyboard.Add(new KeyboardKey("SPACE", Devices.DeviceKeys.SPACE, true, false, 12, 7, 0, 208));
            keyboard.Add(new KeyboardKey("ALT", Devices.DeviceKeys.LEFT_ALT, true, false, 12, 5, 0, 41));
            keyboard.Add(new KeyboardKey("WIN", Devices.DeviceKeys.LEFT_WINDOWS, true, false, 12, 5, 0, 41));
            keyboard.Add(new KeyboardKey("APP", Devices.DeviceKeys.APPLICATION_SELECT, true, false, 12, 5, 0, 41));
            keyboard.Add(new KeyboardKey("CTRL", Devices.DeviceKeys.LEFT_CONTROL, true, false, 12, 5, 0, 50));

            keyboard.Add(new KeyboardKey("LEFT", Devices.DeviceKeys.ARROW_LEFT, true, false, 9, 12));
            keyboard.Add(new KeyboardKey("DOWN", Devices.DeviceKeys.ARROW_DOWN, true, false, 9));
            keyboard.Add(new KeyboardKey("RIGHT", Devices.DeviceKeys.ARROW_DOWN, true, false, 9));

            keyboard.Add(new KeyboardKey("0", Devices.DeviceKeys.NUM_ZERO, true, false, 12, 14, 0, 67));
            keyboard.Add(new KeyboardKey(".", Devices.DeviceKeys.NUM_PERIOD, true, true));

            virtual_keyboard_group = new VirtualGroup(keyboard.ToArray());

            _loaded_localization = PreferredKeyboardLocalization.None;
        }

        public void SetKeyboardColors(Dictionary<Devices.DeviceKeys, System.Drawing.Color> keylights)
        {
            foreach (var kvp in _virtual_keyboard_map)
            {
                if (keylights.ContainsKey(kvp.Key))
                    kvp.Value.SetColor(Utils.ColorUtils.DrawingColorToMediaColor(keylights[kvp.Key]));
            }
        }
    }
}

