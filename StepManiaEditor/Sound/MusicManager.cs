﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FMOD;
using Fumen;

namespace StepManiaEditor;

/// <summary>
/// Class for managing the music sound and preview sound.
/// 
/// Offers idempotent asynchronous methods for loading sound files from disk.
/// 
/// Offers methods for playing the music and setting the music time directly.
/// Gracefully handles desired music times which are negative or outside of the
/// range of the music sound.
/// 
/// Offers methods for playing and stopping the music preview. The music preview
/// may be its own sound from an independent audio file, or it may be a range of
/// the music audio file. When playing the preview sound, it will loop continuously
/// until told to stop.
/// 
/// Offers method for playing the music with an offset. Normally offsets used to sync
/// visuals and audio would be implemented on the visuals by shifting everything up or
/// down to match the audio, however in an Editor the positions need to be precise.
/// If for example the delay between audio and video is substantial and the editor is
/// paused at a specific row, and then playback begins, if the offset were implemented
/// visually, then the notes would visibly jerk. It may be preferable to a user to
/// instead have the audio shift instead.
/// 
/// Expected usage:
///  Call LoadMusicAsync whenever the music file changes.
///  Call LoadMusicPreviewAsync whenever the preview file changes.
///  Call SetPreviewParameters whenever the preview range parameters change.
///  Call Update once each frame.
///  Call StartPlayback and StopPlayback to start and stop playing the music.
///  Call StartPreviewPlayback and StopPreviewPlayback to start and stop playing the preview.
/// </summary>
internal sealed class MusicManager
{
	/// <summary>
	/// Common data to the music and preview Sounds that MusicManager manages.
	/// </summary>
	private class SoundData
	{
		private readonly ChannelGroup ChannelGroup;
		private readonly SoundManager SoundManager;

		// FMOD sound data.
		public Sound Sound;
		public Channel Channel;
		private readonly CHANNELCONTROL_CALLBACK ChannelControlCallback;

		// Cached Sound data for length and time calculations.
		private int NumChannels;
		private int BitsPerSample;
		private uint TotalBytes;
		private uint SampleRate;

		// Loading variables.
		public string PendingFile;
		public string File;
		public CancellationTokenSource LoadCancellationTokenSource;
		public Task LoadTask;

		// Optional SoundMipMap.
		public readonly SoundMipMap MipMap;

		// Whether or not this SoundData is playing. Not necessarily the same
		// as the underlying Sound playing since we may want to play a Sound at
		// a negative time.
		public bool IsPlaying;

		public SoundData(SoundManager soundManager, ChannelGroup channelGroup, SoundMipMap mipMap)
		{
			SoundManager = soundManager;
			ChannelGroup = channelGroup;
			ChannelControlCallback = ChannelCallback;

			if (mipMap != null)
			{
				MipMap = mipMap;
				MipMap.SetLoadParallelism(Preferences.Instance.PreferencesWaveForm.WaveFormLoadingMaxParallelism);
			}
		}

		public void SetSound(Sound sound)
		{
			Sound = sound;

			// Play the sound in order to assign it to a Channel.
			SoundManager.PlaySound(Sound, ChannelGroup, out Channel);

			// Register a callback for the newly assigned Channel so we can respond to it becoming
			// invalid when the sound reaches its end.
			SoundManager.ErrCheck(Channel.setCallback(ChannelControlCallback));

			// Record information about this Sound.
			SoundManager.ErrCheck(Sound.getFormat(out _, out _, out NumChannels, out BitsPerSample));
			SoundManager.ErrCheck(Sound.getLength(out TotalBytes, TIMEUNIT.PCMBYTES));
			SoundManager.ErrCheck(Channel.getFrequency(out var frequency));
			SampleRate = (uint)frequency;
		}

		public uint GetSampleRate()
		{
			return SampleRate;
		}

		public double GetTimeInSeconds()
		{
			if (NumChannels == 0 || SampleRate == 0 || BitsPerSample < 8)
				return 0.0;

			SoundManager.ErrCheck(Channel.getPosition(out var bytes, TIMEUNIT.PCMBYTES));
			return (double)bytes / NumChannels / (BitsPerSample >> 3) / SampleRate;
		}

		public bool SetTimeInSeconds(double timeInSeconds)
		{
			if (!IsLoaded())
				return false;

			// Set the position.
			uint bytes = 0;
			if (timeInSeconds >= 0.0)
				bytes = (uint)(timeInSeconds * NumChannels * (BitsPerSample >> 3) * SampleRate);
			if (bytes >= TotalBytes)
				bytes = TotalBytes - 1;
			SoundManager.ErrCheck(Channel.setPosition(bytes, TIMEUNIT.PCMBYTES));
			return true;
		}

		/// <summary>
		/// Returns whether the sound is at its minimum or maximum position.
		/// Sets the out parameter to the time of the sound in seconds.
		/// This allows for checking if the sound is at its bounds, and getting the time with
		/// one call. With multiple calls, the sound's time may change between calls.
		/// </summary>
		/// <param name="timeInSeconds">Time in seconds of the sound.</param>
		/// <returns>True if the sound at the minimum or maximum position and false otherwise.</returns>
		public bool IsAtMinOrMaxPosition(out double timeInSeconds)
		{
			timeInSeconds = 0.0;
			if (NumChannels == 0 || SampleRate == 0 || BitsPerSample < 8)
				return true;
			SoundManager.ErrCheck(Channel.getPosition(out var bytes, TIMEUNIT.PCMBYTES));

			// Determine the time in seconds to return to the caller.
			timeInSeconds = (double)bytes / NumChannels / (BitsPerSample >> 3) / SampleRate;

			// Consider the sound to be at the maximum position if it is within one sample of the end.
			// In practice calling setPosition on a Channel will result in the position returned by
			// getPosition to be floored to the nearest sample boundary. It is also the case that when
			// playing the sound normally, getPosition will return the total number of bytes when called
			// when the sound has completed, though setPosition cannot be called with that value.
			var sampleSize = NumChannels * (BitsPerSample >> 3);
			return bytes <= 0 || bytes + sampleSize >= TotalBytes;
		}

		public double GetLengthInSeconds()
		{
			if (NumChannels == 0 || SampleRate == 0 || BitsPerSample < 8)
				return 0.0;
			return (double)TotalBytes / NumChannels / (BitsPerSample >> 3) / SampleRate;
		}

		public bool IsLoaded()
		{
			return Sound.hasHandle();
		}

		/// <summary>
		/// Callback from FMOD for responding to Channel events.
		/// </summary>
		private RESULT ChannelCallback(
			IntPtr channelControl,
			CHANNELCONTROL_TYPE controlType,
			CHANNELCONTROL_CALLBACK_TYPE callbackType,
			IntPtr commandData1,
			IntPtr commandData2)
		{
			// When a Channel ends it is no longer valid.
			if (callbackType == CHANNELCONTROL_CALLBACK_TYPE.END)
			{
				// Start playing the sound again to get a new Channel from FMOD.
				SoundManager.PlaySound(Sound, ChannelGroup, out Channel);

				// Set the Channel to be paused at the end of the sound.
				SoundManager.ErrCheck(Channel.setPosition(TotalBytes - 1, TIMEUNIT.PCMBYTES));
				SoundManager.ErrCheck(Channel.setPaused(true));

				// Now that we have a new Channel, set its callback to this function.
				Channel.setCallback(ChannelControlCallback);
			}

			return RESULT.OK;
		}
	}

	/// <summary>
	/// Internal state of the MusicManager.
	/// </summary>
	private enum PlayingState
	{
		PlayingNothing,

		/// <summary>
		/// Playing the song music.
		/// Not necessarily the same as MusicData.IsPlaying since we may be
		/// leveraging the music SoundData to play the preview.
		/// </summary>
		PlayingMusic,

		/// <summary>
		/// Playing the song preview.
		/// Not necessarily the same as PreviewData.IsPlaying since we may be
		/// leveraging the music SoundData to play the preview.
		/// </summary>
		PlayingPreview,
	}

	private readonly SoundManager SoundManager;
	private ChannelGroup MusicChannelGroup;

	// SoundData for both the music and preview.
	private readonly SoundData MusicData;
	private readonly SoundData PreviewData;

	// State.
	private PlayingState State = PlayingState.PlayingNothing;

	// Preview playback variables.
	private bool ShouldBeUsingPreviewFile;
	private Stopwatch PreviewStopwatch;
	private double PreviewStartTime;
	private double PreviewLength;
	private double DesiredMusicTimeAfterPreview;
	private double PreviewFadeInTime;
	private double PreviewFadeOutTime = 1.5;

	/// <summary>
	/// Music and preview sound Channel volume.
	/// </summary>
	private readonly double MusicVolume = 1.0;

	/// <summary>
	/// Internal offset for the music.
	/// The offset is applied to the music but not the preview, as a way of visually synchronizing the music
	/// with the chart as it is played. The preview is played without visuals and should be started precisely
	/// at the specified time.
	/// </summary>
	private double MusicOffset;

	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="soundManager">SoundManager.</param>
	/// <param name="musicOffset">Offset to use for playing the music.</param>
	public MusicManager(SoundManager soundManager, double musicOffset)
	{
		SoundManager = soundManager;

		// Create a ChannelGroup for music and preview audio.
		SoundManager.CreateChannelGroup("MusicChannelGroup", out MusicChannelGroup);

		MusicData = new SoundData(SoundManager, MusicChannelGroup, new SoundMipMap());
		SetMusicOffset(musicOffset);
		PreviewData = new SoundData(SoundManager, MusicChannelGroup, null);
	}

	/// <summary>
	/// Gets the SoundMipMap for the currently loaded music.
	/// </summary>
	/// <returns>SoundMipMap.</returns>
	public SoundMipMap GetMusicMipMap()
	{
		return MusicData.MipMap;
	}

	public void UnloadAsync()
	{
		LoadSoundAsync(PreviewData, null);
		LoadSoundAsync(MusicData, null);
	}

	/// <summary>
	/// Loads the music audio file specified by the given path.
	/// If the given path is empty or negative then it is assumed that no music file
	/// is set.
	/// Will unload any previously loaded music sound file.
	/// Will not reload the previously loaded music file if called again with the same
	/// file.
	/// Will update the music SoundMipMap to reflect the music.
	/// </summary>
	/// <param name="fullPathToMusicFile">
	/// Full path to the audio file representing the music.
	/// </param>
	/// <param name="getMusicTimeFunction">
	/// Function to be called to get the music time so that it can be set appropriate immediately
	/// after loading is complete.
	/// </param>
	/// <param name="force">
	/// If true then the music will be loaded even if the given path is the same
	/// as the previously loaded music. If false, then the sound will not by loaded if
	/// the previously loaded music was from the same path.
	/// </param>
	/// <param name="generateMipMap">
	/// If true then generate a SoundMipMap for the music.
	/// </param>
	public void LoadMusicAsync(
		string fullPathToMusicFile,
		Func<double> getMusicTimeFunction,
		bool force = false,
		bool generateMipMap = true)
	{
		LoadSoundAsync(MusicData, fullPathToMusicFile, MusicOffset, getMusicTimeFunction, force, generateMipMap);
	}

	/// <summary>
	/// Loads the music preview audio file specified by the given path.
	/// If the given path is empty or negative then it is assumed that no preview file
	/// is set and the preview should be determined by a range over the music file.
	/// Will unload any previously loaded preview sound file.
	/// Will not reload the previously loaded preview file if called again with the same
	/// file.
	/// </summary>
	/// <param name="fullPathToMusicFile">
	/// Full path to the audio file representing the preview.
	/// </param>
	/// <param name="force">
	/// If true then the preview music will be loaded even if the given path is the same
	/// as the previously loaded preview. If false, then the sound will not by loaded if
	/// the previously loaded preview was from the same path.
	/// </param>
	public void LoadMusicPreviewAsync(string fullPathToMusicFile, bool force = false)
	{
		// Record that we should be using a unique preview file instead of
		// the music file if we were given a non-empty string.
		ShouldBeUsingPreviewFile = !string.IsNullOrEmpty(fullPathToMusicFile);

		LoadSoundAsync(PreviewData, fullPathToMusicFile, 0.0, null, force);
	}

	/// <summary>
	/// Private helper to asynchronously load a SoundData object.
	/// Could mostly be internal to SoundData, but we set the time internally
	/// based on SetSoundPositionInternal, which takes into account MusicManager state.
	/// We set this before loading the SoundMipMap, in the middle of the async load.
	/// </summary>
	private async void LoadSoundAsync(
		SoundData soundData,
		string fullPathToMusicFile,
		double offset = 0.0,
		Func<double> getMusicTimeFunction = null,
		bool force = false,
		bool generateMipMap = true)
	{
		// It is common for Charts to re-use the same sound files.
		// Do not reload the sound file if we were already using it.
		if (!force && fullPathToMusicFile == soundData.File)
			return;

		// Store the sound file we want to load.
		soundData.PendingFile = fullPathToMusicFile;

		// If we are already loading a sound file, cancel that operation so
		// we can start the new load.
		if (soundData.LoadCancellationTokenSource != null)
		{
			// If we are already cancelling then return. We don't want multiple
			// calls to this method to collide. We will end up using the most recently
			// requested sound file due to the PendingFile variable.
			if (soundData.LoadCancellationTokenSource.IsCancellationRequested)
				return;

			// Start the cancellation and wait for it to complete.
			soundData.LoadCancellationTokenSource?.Cancel();
			await soundData.LoadTask;
		}

		// Store the new sound file.
		soundData.File = soundData.PendingFile;
		soundData.PendingFile = null;

		// Start an asynchronous series of operations to load the sound and set up the mip map.
		soundData.LoadCancellationTokenSource = new CancellationTokenSource();
		soundData.LoadTask = Task.Run(async () =>
		{
			try
			{
				// Release the handle to the old sound if it is present.
				if (soundData.Sound.hasHandle())
					SoundManager.ErrCheck(soundData.Sound.release());
				soundData.Sound.handle = IntPtr.Zero;

				// Reset the mip map before loading the new sound because loading the sound
				// can take a moment and we don't want to continue to render the old audio.
				soundData.MipMap?.Reset();

				soundData.LoadCancellationTokenSource.Token.ThrowIfCancellationRequested();

				if (!string.IsNullOrEmpty(soundData.File))
				{
					Logger.Info($"Loading {soundData.File}...");

					// Load the sound file.
					// This is not cancelable. According to FMOD: "you can't cancel it"
					// https://qa.fmod.com/t/reusing-channels/13145/3
					// Normally this is not a problem, but for hour-long files this is unfortunate.
					soundData.SetSound(await SoundManager.LoadAsync(soundData.File));

					if (getMusicTimeFunction != null)
						SetSoundPositionInternal(soundData, getMusicTimeFunction(), offset);
					Logger.Info($"Loaded {soundData.File}.");

					soundData.LoadCancellationTokenSource.Token.ThrowIfCancellationRequested();

					// Set up the new sound mip map.
					if (generateMipMap && soundData.MipMap != null)
					{
						await soundData.MipMap.CreateMipMapAsync(soundData.Sound, soundData.GetSampleRate(),
							Utils.WaveFormTextureWidth,
							soundData.LoadCancellationTokenSource.Token);
					}

					soundData.LoadCancellationTokenSource.Token.ThrowIfCancellationRequested();
				}
			}
			catch (OperationCanceledException)
			{
				// Upon cancellation release the sound handle and clear the mip map data.
				if (soundData.Sound.hasHandle())
					SoundManager.ErrCheck(soundData.Sound.release());
				soundData.Sound.handle = IntPtr.Zero;
				soundData.MipMap?.Reset();
			}
			finally
			{
				soundData.LoadCancellationTokenSource?.Dispose();
				soundData.LoadCancellationTokenSource = null;
			}
		}, soundData.LoadCancellationTokenSource.Token);
		await soundData.LoadTask;
	}

	/// <summary>
	/// Returns whether or not the music sound is loaded.
	/// </summary>
	/// <returns>Whether or not the music sound is loaded.</returns>
	public bool IsMusicLoaded()
	{
		return MusicData.IsLoaded();
	}

	/// <summary>
	/// Returns whether the music is at its minimum or maximum position.
	/// Sets the out parameter to the time of the music in seconds.
	/// This allows for checking if the sound is at its bounds, and getting the time with
	/// one call. With multiple calls, the sound's time may change between calls.
	/// </summary>
	/// <param name="timeInSeconds">Time in seconds of the music.</param>
	/// <returns>True if the music at the minimum or maximum position and false otherwise.</returns>
	public bool IsMusicAtMinOrMaxPosition(out double timeInSeconds)
	{
		var result = MusicData.IsAtMinOrMaxPosition(out timeInSeconds);
		timeInSeconds -= MusicOffset;
		return result;
	}

	/// <summary>
	/// Sets the music to the given time in seconds.
	/// If a preview is playing that uses the music file, then the given value will
	/// be set after the preview is stopped.
	/// The given time my be negative or outside the time range of the music sound.
	/// </summary>
	/// <param name="musicTimeInSeconds">The desired music time in seconds.</param>
	public void SetMusicTimeInSeconds(double musicTimeInSeconds)
	{
		// If we are using the music file to play a preview we do not
		// want to set the position as it will interfere with the preview playback.
		// Rather than require the caller to check this, just store the desired
		// time and set it after the preview completes.
		if (State == PlayingState.PlayingPreview)
		{
			DesiredMusicTimeAfterPreview = musicTimeInSeconds;
			return;
		}

		SetSoundPositionInternal(MusicData, musicTimeInSeconds, MusicOffset);
	}

	/// <summary>
	/// Private internal method for setting the music sound to a desired time in seconds.
	/// Used for both setting the time for the music and for the preview when the preview
	/// uses the music file instead of an independent preview file.
	/// The given time my be negative or outside the time range of the music sound.
	/// </summary>
	/// <param name="soundData">Sound data to set the time on.</param>
	/// <param name="timeInSeconds">Sound time in seconds.</param>
	/// <param name="offset">Offset to be added to the time.</param>
	private void SetSoundPositionInternal(SoundData soundData, double timeInSeconds, double offset)
	{
		if (!soundData.SetTimeInSeconds(timeInSeconds + offset))
			return;
		if (!soundData.IsPlaying)
			return;
		if (soundData == MusicData && State == PlayingState.PlayingPreview && ShouldBeUsingPreviewFile)
			return;
		UpdateSoundPausedState(soundData, timeInSeconds + offset);
	}

	/// <summary>
	/// Sets parameters used for playing the preview when it uses the music file instead
	/// of an independent file.
	/// </summary>
	/// <param name="startTime">Start time of preview in seconds.</param>
	/// <param name="length">Length of preview in seconds.</param>
	/// <param name="fadeInTime">Time in seconds over which to fade in the preview when playing.</param>
	/// <param name="fadeOutTime">Time in seconds over which to fade out the preview when playing.</param>
	public void SetPreviewParameters(double startTime, double length, double fadeInTime, double fadeOutTime)
	{
		PreviewStartTime = startTime;
		PreviewLength = length;
		PreviewFadeInTime = fadeInTime;
		PreviewFadeOutTime = fadeOutTime;
	}

	/// <summary>
	/// Start playing the music.
	/// </summary>
	/// <param name="musicTimeInSeconds">
	/// Desired music time in seconds.
	/// Can be negative and outside the time range of the music sound.
	/// </param>
	public void StartPlayback(double musicTimeInSeconds)
	{
		System.Diagnostics.Debug.Assert(State == PlayingState.PlayingNothing || State == PlayingState.PlayingMusic);

		State = PlayingState.PlayingMusic;
		MusicData.IsPlaying = true;
		UpdateSoundPausedState(MusicData, musicTimeInSeconds + MusicOffset);
	}

	/// <summary>
	/// Stop playing the music.
	/// </summary>
	public void StopPlayback()
	{
		State = PlayingState.PlayingNothing;
		MusicData.IsPlaying = false;
		if (MusicData.IsLoaded())
			SoundManager.ErrCheck(MusicData.Channel.setPaused(true));
	}

	/// <summary>
	/// Start playing the preview.
	/// If the last call to LoadMusicPreviewAsync was for a non null and non empty file string,
	/// then the preview is assumed to be the audio file specified by that path.
	/// If the last call to LoadMusicPreviewAsync was for a null or empty file string, then the
	/// preview is assumed to be the music file using a range defined by the parameters set
	/// in SetPreviewParameters.
	/// </summary>
	/// <returns>
	/// True if the preview was successfully started and false otherwise.
	/// The preview may fail to play if the audio did not load successfully
	/// or an invalid preview range is defined.
	/// </returns>
	public bool StartPreviewPlayback()
	{
		System.Diagnostics.Debug.Assert(State == PlayingState.PlayingNothing);

		var soundData = GetPreviewSoundData();

		// If the sound file is not loaded yet, just ignore the request.
		if (!soundData.IsLoaded())
			return false;

		// Don't play anything if the range is not valid. We want to avoid
		// a buzzing artifact by looping every frame.
		if (!ShouldBeUsingPreviewFile && PreviewLength <= 0.0)
			return false;

		DesiredMusicTimeAfterPreview = MusicData.GetTimeInSeconds() - MusicOffset;
		State = PlayingState.PlayingPreview;

		RestartPreview();
		return true;
	}

	/// <summary>
	/// Restart the preview sound either due to it starting playback for
	/// the first time, or needed to restart due to looping.
	/// </summary>
	private void RestartPreview()
	{
		var soundData = GetPreviewSoundData();
		soundData.IsPlaying = true;
		var previewStartTime = ShouldBeUsingPreviewFile ? 0.0 : PreviewStartTime;
		SetSoundPositionInternal(soundData, previewStartTime, 0.0);
		PreviewStopwatch = Stopwatch.StartNew();
	}

	/// <summary>
	/// Stop playing the preview.
	/// </summary>
	public void StopPreviewPlayback()
	{
		System.Diagnostics.Debug.Assert(State == PlayingState.PlayingPreview);

		// Stop playing the preview.
		var previewSoundData = GetPreviewSoundData();
		if (previewSoundData.IsLoaded())
			SoundManager.ErrCheck(previewSoundData.Channel.setPaused(true));

		State = PlayingState.PlayingNothing;
		previewSoundData.IsPlaying = false;
		PreviewStopwatch?.Stop();
		PreviewStopwatch = null;

		// When stopping the preview, set the music position back to what it should
		// be set to.
		SetMusicTimeInSeconds(DesiredMusicTimeAfterPreview);

		// Reset the music volume in case it was fading out due to the preview.
		SoundManager.ErrCheck(previewSoundData.Channel.setVolume((float)MusicVolume));
	}

	/// <summary>
	/// Perform time-dependent updates.
	/// </summary>
	/// <param name="musicTimeInSeconds">The desired music time in seconds.</param>
	public void Update(double musicTimeInSeconds)
	{
		// If the music is playing, then we need to update the paused state based on whether
		// the desired music time is a valid time to set the music to.
		if (State == PlayingState.PlayingMusic)
		{
			UpdateSoundPausedState(MusicData, musicTimeInSeconds + MusicOffset);
		}

		// If playing the preview we are either playing a sample range of the music file, or
		// playing an independent preview file. In both cases, fading and looping of the preview
		// is controlled here.
		if (State == PlayingState.PlayingPreview)
		{
			var soundData = GetPreviewSoundData();
			var previewTime = PreviewStopwatch.Elapsed.TotalSeconds;
			var previewStartTime = ShouldBeUsingPreviewFile ? 0.0 : PreviewStartTime;
			var previewLength = ShouldBeUsingPreviewFile ? soundData.GetLengthInSeconds() : PreviewLength;

			// If the preview is configured to begin before the music starts, check
			// for starting the playback.
			if (previewStartTime < 0.0)
			{
				if (previewTime > -previewStartTime)
				{
					UpdateSoundPausedState(soundData, 0.0);
				}
			}

			// Fade preview music in and out.
			if (previewTime > previewLength - PreviewFadeOutTime)
			{
				var vol = (float)Interpolation.Lerp(
					(float)MusicVolume, 0.0, 0.0, PreviewFadeOutTime,
					previewTime - (previewLength - PreviewFadeOutTime));
				SoundManager.ErrCheck(soundData.Channel.setVolume(vol));
			}
			else if (previewTime < PreviewFadeInTime)
			{
				var vol = (float)Interpolation.Lerp(
					0.0, (float)MusicVolume, 0.0, PreviewFadeInTime, previewTime);
				SoundManager.ErrCheck(soundData.Channel.setVolume(vol));
			}
			else
			{
				SoundManager.ErrCheck(soundData.Channel.setVolume((float)MusicVolume));
			}

			// Loop.
			if (previewTime > previewLength)
			{
				RestartPreview();
			}
		}
	}

	/// <summary>
	/// Sets the given SoundData to be paused or unpaused based on the desired
	/// time.
	/// FMOD can't set a Sound Channel time to less than 0.0, however the
	/// desired time may be less than 0.0. To work around this behavior, we
	/// pause the channel when wanting to behave like it is before 0.0, and
	/// then unpause it when it reaches 0.0.
	/// </summary>
	/// <param name="soundData">SoundData to pause or unpause.</param>
	/// <param name="musicTimeInSeconds">Desired sound time in seconds.</param>
	private void UpdateSoundPausedState(SoundData soundData, double musicTimeInSeconds)
	{
		// Early out if the sound is not loaded yet.
		if (!soundData.IsLoaded())
			return;

		// Do not affect the sound if we are playing a preview using a unique preview file
		// and the sound provided above is the music sound data. We do not want to unpause
		// it while the preview is playing.
		if (soundData == MusicData && State == PlayingState.PlayingPreview && ShouldBeUsingPreviewFile)
			return;

		SoundManager.ErrCheck(soundData.Channel.setPaused(musicTimeInSeconds < 0.0));
	}

	/// <summary>
	/// Gets the length of the music in seconds.
	/// </summary>
	/// <returns>Length of the music in seconds.</returns>
	public double GetMusicLengthInSeconds()
	{
		return MusicData.GetLengthInSeconds();
	}

	/// <summary>
	/// Gets the appropriate SoundData object to use for the preview based on whether
	/// we should be using a unique preview file or the music sound file.
	/// </summary>
	/// <returns></returns>
	private SoundData GetPreviewSoundData()
	{
		return ShouldBeUsingPreviewFile ? PreviewData : MusicData;
	}

	/// <summary>
	/// Sets the offset to be used when playing the music.
	/// Changes to this value will not have an effect until the next time the music is played.
	/// </summary>
	/// <param name="offset">New music offset value.</param>
	public void SetMusicOffset(double offset)
	{
		MusicOffset = offset;
	}

	/// <summary>
	/// Sets the Volume all sounds managed through this MusicManager.
	/// </summary>
	/// <param name="volume">Volume as a value between 0.0f and 1.0f.</param>
	public void SetVolume(float volume)
	{
		// Set the volume on the MusicChannelGroup to control all sounds.
		SoundManager.ErrCheck(MusicChannelGroup.setVolume(volume));
	}
}
