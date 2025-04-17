using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace GameTools
{
    public class HotkeyManager
    {
        // Windows API 常量和导入
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // 修饰键常量
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private const uint MOD_NOREPEAT = 0x4000;

        // 热键ID计数器
        private static int _hotKeyId = 0;
        
        // 热键字典
        private Dictionary<int, HotKeyInfo> _registeredHotkeys = new Dictionary<int, HotKeyInfo>();
        
        // 窗口句柄
        private IntPtr _windowHandle;
        private HwndSource _source;
        
        // 热键信息类
        private class HotKeyInfo
        {
            public Action? Callback { get; set; }
            public uint Modifiers { get; set; }
            public uint Key { get; set; }
        }

        public HotkeyManager(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source.AddHook(WndProc);
        }

        // 注册热键
        public bool RegisterHotKey(Key key, bool ctrl = false, bool alt = false, bool shift = false, bool win = false, Action? callback = null)
        {
            uint modifiers = 0;
            if (ctrl) modifiers |= MOD_CONTROL;
            if (alt) modifiers |= MOD_ALT;
            if (shift) modifiers |= MOD_SHIFT;
            if (win) modifiers |= MOD_WIN;
            modifiers |= MOD_NOREPEAT;

            int id = _hotKeyId++;
            uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);

            bool result = RegisterHotKey(_windowHandle, id, modifiers, vk);
            if (result)
            {
                _registeredHotkeys[id] = new HotKeyInfo
                {
                    Callback = callback,
                    Modifiers = modifiers,
                    Key = vk
                };
            }

            return result;
        }

        // 注销所有热键
        public void UnregisterAllHotKeys()
        {
            foreach (int id in _registeredHotkeys.Keys)
            {
                UnregisterHotKey(_windowHandle, id);
            }
            _registeredHotkeys.Clear();
        }

        // 窗口过程处理热键消息
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (_registeredHotkeys.TryGetValue(id, out var hotKeyInfo) && hotKeyInfo != null)
                {
                    hotKeyInfo.Callback?.Invoke();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }
    }
} 