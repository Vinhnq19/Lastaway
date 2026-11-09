using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.Lobby
{
    /// <summary>
    /// "Cài đặt · Máy chủ" modal (Figma 05_settings-server 133:195): the old Home endpoint picker plus Close, opened from
    /// Home "Cài đặt" and Login "Máy chủ (nâng cao)". Audio opens the UI-03 settings panel (Home only). References only.
    /// </summary>
    public sealed class ServerSettingsView : LobbyPanelView
    {
        [SerializeField] private TMP_Dropdown profile;
        [SerializeField] private TMP_InputField host;
        [SerializeField] private TMP_InputField port;
        [SerializeField] private TMP_InputField api;
        [SerializeField] private Button save;
        [SerializeField] private TMP_Text status;
        [SerializeField] private Button audioSettings;
        [SerializeField] private Button close;

        [SerializeField] private RectTransform popup;
        [SerializeField] private GameObject normalFields;
        [SerializeField] private GameObject vpnPanel;
        [SerializeField] private TMP_InputField vpnCode;
        [SerializeField] private TMP_Text vpnStatus;
        [SerializeField] private TMP_Text vpnBackend;
        [SerializeField] private Button vpnConnect, vpnClientSetup, vpnBrowse, vpnSetup, vpnStart, vpnStop, vpnRefresh, vpnCopy;
        [SerializeField] private RectTransform popupFooter, popupTitle, popupClose;
        private Vector2 _popupSize, _closePosition, _profileSize, _footerSize, _footerPosition, _titleSize, _xPosition;
        private bool _sizesRead;
        public TMP_InputField VpnCode => vpnCode;
        public TMP_Text VpnStatus => vpnStatus;
        public TMP_Text VpnBackend => vpnBackend;
        public Button VpnConnect => vpnConnect;
        public Button VpnClientSetup => vpnClientSetup;
        public Button VpnBrowse => vpnBrowse;
        public Button VpnSetup => vpnSetup;
        public Button VpnStart => vpnStart;
        public Button VpnStop => vpnStop;
        public Button VpnRefresh => vpnRefresh;
        public Button VpnCopy => vpnCopy;
        public void ShowVpn(bool visible)
        {
            if (vpnPanel == null || popup == null) return;
            if (!_sizesRead)
            {
                _popupSize = popup.sizeDelta;
                _closePosition = ((RectTransform)close.transform).anchoredPosition;
                _profileSize = ((RectTransform)profile.transform).sizeDelta;
                _footerSize = popupFooter.sizeDelta; _footerPosition = popupFooter.anchoredPosition;
                _titleSize = popupTitle.sizeDelta; _xPosition = popupClose.anchoredPosition;
                _sizesRead = true;
            }
            var scale = _popupSize.x / 820f;
            popup.sizeDelta = visible ? new Vector2(1220, 1040) * scale : _popupSize;
            ((RectTransform)close.transform).anchoredPosition = _closePosition + (visible ? new Vector2(200, -320) * scale : Vector2.zero);
            ((RectTransform)profile.transform).sizeDelta = _profileSize + (visible ? new Vector2(400, 0) * scale : Vector2.zero);
            popupFooter.sizeDelta = _footerSize + (visible ? new Vector2(400, 0) * scale : Vector2.zero);
            popupFooter.anchoredPosition = _footerPosition + (visible ? new Vector2(0, -320) * scale : Vector2.zero);
            popupTitle.sizeDelta = _titleSize + (visible ? new Vector2(400, 0) * scale : Vector2.zero);
            popupClose.anchoredPosition = _xPosition + (visible ? new Vector2(400, 0) * scale : Vector2.zero);
            normalFields.SetActive(!visible);
            vpnPanel.SetActive(visible);
        }

        public TMP_Dropdown Profile => profile;
        public TMP_InputField Host => host;
        public TMP_InputField Port => port;
        public TMP_InputField Api => api;
        public Button Save => save;
        public TMP_Text Status => status;
        public Button Audio => audioSettings;
        public Button Close => close;
    }
}
