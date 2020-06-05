using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_UI
{
    class Setting
    {
        public int LanguageComboBox_SelectedIndex { get; set; }
        public string VRMPathTextBox_Text { get; set; }
        public string BackgroundObjectPathTextBox_Text { get; set; }
        public double CameraRotateXSlider_Value { get; set; }
        public double CameraRotateYSlider_Value { get; set; }
        public double CameraRotateZSlider_Value { get; set; }
        public double CameraValue1Slider_Value { get; set; }
        public double CameraValue2Slider_Value { get; set; }
        public double CameraValue3Slider_Value { get; set; }
        public double LightRotateXSlider_Value { get; set; }
        public double LightRotateYSlider_Value { get; set; }
        public double LightRotateZSlider_Value { get; set; }
        public double LightValue1Slider_Value { get; set; }
        public double LightValue2Slider_Value { get; set; }
        public double LightValue3Slider_Value { get; set; }
        public bool LightTypeDirectionalRadioButton_IsChecked_Value { get; set; }
        public bool LightTypePointRadioButton_IsChecked_Value { get; set; }
        public bool LightTypeSpotRadioButton_IsChecked_Value { get; set; }
        public double BackgroundRotateXSlider_Value { get; set; }
        public double BackgroundRotateYSlider_Value { get; set; }
        public double BackgroundRotateZSlider_Value { get; set; }
        public double BackgroundValue1Slider_Value { get; set; }
        public double BackgroundValue2Slider_Value { get; set; }
        public double BackgroundValue3Slider_Value { get; set; }
        public double BackgroundScaleSlider_Value { get; set; }
        [DefaultValue(true)]
        public bool EVMC4UEnableCheckBox_IsChecked_Value { get; set; }
        [DefaultValue(39540)]
        public string EVMC4UPortTextBox_Text { get; set; }
        public bool EVMC4UFreezeCheckBox_IsChecked_Value { get; set; }
        public bool EVMC4UBoneFilterCheckBox_IsChecked_Value { get; set; }
        public bool EVMC4UBlendShapeFilterCheckBox_IsChecked_Value { get; set; }
        public string EVMC4UBoneFilterValueTextBox_Text { get; set; }
        public string EVMC4UBlendShapeFilterValueTextBox_Text { get; set; }
        public bool WindowOptionWindowBorderCheckBox_IsChecked_Value { get; set; }
        public bool WindowOptionForceForegroundCheckBox_IsChecked_Value { get; set; }
        public bool WindowOptionTransparentCheckBox_IsChecked_Value { get; set; }
        public bool CameraRootPosLockCheckBox_IsChecked_Value { get; set; }
        public bool LightRootPosLockCheckBox_IsChecked_Value { get; set; }
        public bool BackgroundRootPosLockCheckBox_IsChecked_Value { get; set; }
        public bool OBSExternalControl_CheckBox_IsChecked_Value { get; set; }
        public bool UnityCaptureEnable_CheckBox_IsChecked_Value { get; set; }
        public string SEDSSServerPasswordTextBox_Password { get; set; }
        public string SEDSSServerExchangeFilePathTextBox_Text { get; set; }
        public string SEDSSClientAddressTextBox_Text { get; set; }
        public string SEDSSClientPortTextBox_Text { get; set; }
        public string SEDSSClientPasswordTextBox_Password { get; set; }
        public string SEDSSClientIDTextBox_Text { get; set; }
        public string SEDSSClientUploadFilePathTextBox_Text { get; set; }
        public byte BackgroundColorPicker_SelectedColor_R { get; set; }
        public byte BackgroundColorPicker_SelectedColor_G { get; set; }
        public byte BackgroundColorPicker_SelectedColor_B { get; set; }
        public byte BackgroundColorPicker_SelectedColor_A { get; set; }
        public byte LightColorPicker_SelectedColor_R { get; set; }
        public byte LightColorPicker_SelectedColor_G { get; set; }
        public byte LightColorPicker_SelectedColor_B { get; set; }
        public byte LightColorPicker_SelectedColor_A { get; set; }
        public byte EnvironmentColorPicker_SelectedColor_R { get; set; }
        public byte EnvironmentColorPicker_SelectedColor_G { get; set; }
        public byte EnvironmentColorPicker_SelectedColor_B { get; set; }
        public byte EnvironmentColorPicker_SelectedColor_A { get; set; }
        public bool PostProcessingAntiAliasingEnableCheckBox_IsChecked_Value { get; set; }
        public bool PostProcessingBloomEnableCheckBox_IsChecked_Value { get; set; }
        public double PostProcessingBloomIntensitySlider_Value { get; set; }
        public double PostProcessingBloomThresholdSlider_Value { get; set; }
        public bool PostProcessingDoFEnableCheckBox_IsChecked_Value { get; set; }
        public double PostProcessingDoFFocusDistanceSlider_Value { get; set; }
        public double PostProcessingDoFApertureSlider_Value { get; set; }
        public double PostProcessingDoFFocusLengthSlider_Value { get; set; }
        public double PostProcessingDoFMaxBlurSizeSlider_Value { get; set; }
        public bool PostProcessingCGEnableCheckBox_IsChecked_Value { get; set; }
        public double PostProcessingCGTemperatureSlider_Value { get; set; }
        public double PostProcessingCGSaturationSlider_Value { get; set; }
        public double PostProcessingCGContrastSlider_Value { get; set; }
        public bool PostProcessingVEnableCheckBox_IsChecked_Value { get; set; }
        public double PostProcessingVIntensitySlider_Value { get; set; }
        public double PostProcessingVSmoothnessSlider_Value { get; set; }
        public double PostProcessingVRoundedSlider_Value { get; set; }
        public bool PostProcessingCAEnableCheckBox_IsChecked_Value { get; set; }
        public double PostProcessingCAIntensitySlider_Value { get; set; }
    }
}
