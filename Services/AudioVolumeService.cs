using System.Runtime.InteropServices;

namespace VolKey.Services;

/// <summary>Уровень и состояние «без звука» устройства вывода.</summary>
internal readonly record struct VolumeState(float Level, bool Muted);

/// <summary>Читает и восстанавливает громкость активного устройства вывода Windows.</summary>
internal sealed class AudioVolumeService : IDisposable
{
    private const int ClsctxAll = 23;
    private IMMDeviceEnumerator? _enumerator;

    /// <summary>Достаточно быстрый, чтобы вызываться из хука клавиатуры: перечислитель устройств создаётся один раз.</summary>
    public bool TryGetState(out VolumeState state)
    {
        state = default;
        IAudioEndpointVolume? endpointVolume = null;
        try
        {
            endpointVolume = GetEndpointVolume();
            if (endpointVolume.GetMasterVolumeLevelScalar(out var level) < 0 ||
                endpointVolume.GetMute(out var muted) < 0)
                return false;
            state = new VolumeState(level, muted);
            return true;
        }
        catch (Exception ex) when (ex is COMException or InvalidCastException)
        {
            ResetEnumerator();
            return false;
        }
        finally
        {
            if (endpointVolume is not null) Marshal.ReleaseComObject(endpointVolume);
        }
    }

    /// <summary>Устанавливает уровень и/или режим «без звука»; null оставляет значение как есть.</summary>
    public void Apply(float? level, bool? muted)
    {
        IAudioEndpointVolume? endpointVolume = null;
        try
        {
            endpointVolume = GetEndpointVolume();
            if (level.HasValue) endpointVolume.SetMasterVolumeLevelScalar(level.Value, Guid.Empty);
            if (muted.HasValue) endpointVolume.SetMute(muted.Value, Guid.Empty);
        }
        catch (Exception ex) when (ex is COMException or InvalidCastException)
        {
            // При смене устройства воспроизведения восстановление можно пропустить.
            ResetEnumerator();
        }
        finally
        {
            if (endpointVolume is not null) Marshal.ReleaseComObject(endpointVolume);
        }
    }

    public void Dispose() => ResetEnumerator();

    private IAudioEndpointVolume GetEndpointVolume()
    {
        _enumerator ??= (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
        IMMDevice? device = null;
        try
        {
            // Устройство запрашиваем каждый раз: пользователь мог переключить вывод на другое.
            ThrowIfFailed(_enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out device));
            var interfaceId = typeof(IAudioEndpointVolume).GUID;
            ThrowIfFailed(device.Activate(ref interfaceId, ClsctxAll, IntPtr.Zero, out var endpoint));
            return (IAudioEndpointVolume)endpoint;
        }
        finally
        {
            if (device is not null) Marshal.ReleaseComObject(device);
        }
    }

    private void ResetEnumerator()
    {
        if (_enumerator is not null) Marshal.ReleaseComObject(_enumerator);
        _enumerator = null;
    }

    private static void ThrowIfFailed(int hresult)
    {
        if (hresult < 0) throw new COMException(null, hresult);
    }

    private enum EDataFlow { eRender, eCapture, eAll }
    private enum ERole { eConsole, eMultimedia, eCommunications }

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumeratorComObject { }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(EDataFlow dataFlow, int stateMask, out IntPtr devices);
        [PreserveSig] int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice device);
        [PreserveSig] int GetDevice(string id, out IMMDevice device);
        [PreserveSig] int RegisterEndpointNotificationCallback(IntPtr client);
        [PreserveSig] int UnregisterEndpointNotificationCallback(IntPtr client);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid interfaceId, int classContext, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object endpointInterface);
        [PreserveSig] int OpenPropertyStore(int accessMode, out IntPtr properties);
        [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
        [PreserveSig] int GetState(out int state);
    }

    [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        [PreserveSig] int RegisterControlChangeNotify(IntPtr notify);
        [PreserveSig] int UnregisterControlChangeNotify(IntPtr notify);
        [PreserveSig] int GetChannelCount(out uint channelCount);
        [PreserveSig] int SetMasterVolumeLevel(float levelDb, Guid eventContext);
        [PreserveSig] int SetMasterVolumeLevelScalar(float level, Guid eventContext);
        [PreserveSig] int GetMasterVolumeLevel(out float levelDb);
        [PreserveSig] int GetMasterVolumeLevelScalar(out float level);
        [PreserveSig] int SetChannelVolumeLevel(uint channel, float levelDb, Guid eventContext);
        [PreserveSig] int SetChannelVolumeLevelScalar(uint channel, float level, Guid eventContext);
        [PreserveSig] int GetChannelVolumeLevel(uint channel, out float levelDb);
        [PreserveSig] int GetChannelVolumeLevelScalar(uint channel, out float level);
        [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, Guid eventContext);
        [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
        [PreserveSig] int GetVolumeStepInfo(out uint step, out uint stepCount);
        [PreserveSig] int VolumeStepUp(Guid eventContext);
        [PreserveSig] int VolumeStepDown(Guid eventContext);
        [PreserveSig] int QueryHardwareSupport(out uint hardwareSupportMask);
        [PreserveSig] int GetVolumeRange(out float volumeMinDb, out float volumeMaxDb, out float volumeIncrementDb);
    }
}
