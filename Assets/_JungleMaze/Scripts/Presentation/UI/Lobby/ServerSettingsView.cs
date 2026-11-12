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
        [SerializeField] private TMP_Dropdown lanInterface, devProfile;
        [SerializeField] private TMP_Text serverDescription;
        [SerializeField] private Button trustConfirm, trustCancel;
        public TMP_Dropdown LanInterface => lanInterface;
        public TMP_Dropdown DevProfile => devProfile;
        public Button TrustConfirm => trustConfirm;
        public Button TrustCancel => trustCancel;
        public void ShowMode(int mode, bool confirm, bool advanced)
        {
            ShowVpn(mode != 0);
            // Reserve the dev picker row even before a dev profile has been selected.
            if(mode==0 && Debug.isDebugBuild && popup!=null && _sizesRead)
            {
                var scale=_popupSize.x/820f;
                popup.sizeDelta=_popupSize+new Vector2(0,140)*scale;
                ((RectTransform)close.transform).anchoredPosition=_closePosition+new Vector2(0,-140)*scale;
                popupFooter.anchoredPosition=_footerPosition+new Vector2(0,-140)*scale;
            }
            if (lanInterface != null) lanInterface.gameObject.SetActive(mode == 1);
            if (trustConfirm != null) trustConfirm.gameObject.SetActive(mode == 1 && confirm);
            if (trustCancel != null) trustCancel.gameObject.SetActive(mode == 1 && confirm);
            if (devProfile != null) devProfile.gameObject.SetActive(mode == 0 && Debug.isDebugBuild);
            if (host != null) host.gameObject.SetActive(mode == 0 && advanced);
            if (port != null) port.gameObject.SetActive(mode == 0 && advanced);
            if (api != null) api.gameObject.SetActive(mode == 0 && advanced);
            if (serverDescription != null) serverDescription.gameObject.SetActive(mode == 0 && !advanced);
            if(vpnCode != null && vpnCode.placeholder is TMP_Text placeholder)
                placeholder.text=mode==1 ? "Dán mã do chủ máy sao chép" : "VD: may-host.tail123";
            var panel = vpnPanel != null ? vpnPanel.transform : null;
            var help = panel != null ? panel.Find("ClientHelp")?.GetComponent<TMP_Text>() : null;
            if (help != null) help.text = mode == 1
                ? "Nhận mã từ chủ máy cùng mạng, dán và kết nối.\nLần đầu cần xác nhận máy chủ.\nMã này khác mã phòng; không cần cài thêm ứng dụng."
                : "Nhận lời mời Tailscale và đăng nhập ứng dụng.\nDán mã kết nối rồi đăng nhập game.\nMã này khác mã phòng.";
            var hostHelp = panel != null ? panel.Find("HostHelp") : null;
            if (hostHelp != null) hostHelp.gameObject.SetActive(mode != 1);
            var title = panel != null ? panel.Find("HostTitle")?.GetComponent<TMP_Text>() : null;
            if (title != null) title.text = "Tự làm máy chủ";
            Label(vpnClientSetup, mode == 1 ? "Dán mã" : "Cài / đăng nhập Tailscale");
            Label(save, advanced ? "Lưu (dev)" : "Kết nối AWS");
        }
        private static void Label(Button button, string value)
        { if (button != null) { var text=button.GetComponentInChildren<TMP_Text>(); if(text!=null)text.text=value; } }
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

        private void Awake()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            if (api != null) api.gameObject.SetActive(false);
            if (port != null) port.gameObject.SetActive(false);
#endif
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
