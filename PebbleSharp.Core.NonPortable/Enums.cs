﻿using System;

namespace PebbleSharp.Core
{
    /// <summary> Media control instructions as understood by Pebble </summary>
    public enum MediaControl : byte
    {
        None = 0,
        PlayPause = 1,
        Pause = 2,
        Play = 3,
        Next = 4,
        Previous = 5,
        VolumeUp = 6,
        VolumeDown = 7,
        GetNowPlaying = 8,
        SendNowPlaying = 9
    }

	/// <summary>
	///     Endpoints (~"commands") used by Pebble to indicate particular instructions
	///     or instruction types.
	/// </summary>
	public enum Endpoint : ushort
	{
		Firmware = 1,
		Time = 11,
		FirmwareVersion = 16,
		PhoneVersion = 17,
		SystemMessage = 18,
		MusicControl = 32,
		PhoneControl = 33,
		ApplicationMessage = 48,
		Launcher = 49,
		AppCustomize = 50,
		AppRunState = 52,
		Logs = 2000,
		Ping = 2001,
		LogDump = 2002,
		Reset = 2003,
		App = 2004,
		Mfg = 2004,
		AppLogs = 2006,
		Notification = 3000,
		Resource = 4000,
		SysReg = 5000,
		FctReg = 5001,
		AppManager = 6000,
		AppFetch = 6001,
		RunKeeper = 7000,
		PutBytes = 48879,
		DataLog = 6778,
		CoreDump = 9000,
		BlobDB = 45531,//0xb1db
		MaxEndpoint = 65535, //ushort.MaxValue

	}

    public enum LogLevel
    {
        Unknown = -1,
        All = 0,
        Error = 1,
        Warning = 50,
        Information = 100,
        Debug = 200,
        Verbose = 250
    }

    public enum AppMessage : byte
    {
        Push = 0x01,
        Request = 0x02,
        Ack = 0xFF,
        Nack = 0x7F
    }

	public enum BlobDatabase:byte
	{
		Test = 0,
		Pin = 1,
		App = 2,
		Reminder = 3,
		Notification = 4
	}

	public enum BlobStatus:byte
	{
		Success = 0x01,
		GeneralFailure = 0x02,
		InvalidOperation = 0x03,
		InvalidDatabaseID = 0x04,
		InvalidData = 0x05,
		KeyDoesNotExist = 0x06,
		DatabaseFull = 0x07,
		DataStale = 0x08
	}

	public enum BlobCommand:byte
	{
		Insert=0x01,
		Delete=0x04,
		Clear=0x05,
	}

	public enum AppRunState : byte
	{
		Start=0x01,
		Stop=0x02,
		Request=0x03,
	}

	public enum AppFetchStatus : byte
	{
		Start = 0x01,
		Busy = 0x02,
		InvalidUUID = 0x03,
		NoData = 0x04
	}


	public enum TransferType : byte
	{
		Firmware = 1,
		Recovery = 2,
		SysResources = 3,
		Resources = 4,
		Binary = 5,
		File=6,
		Worker=7
	}

	public enum SystemMessage : byte
	{
		FirmwareAvailible = 0,
		FirmwareStart = 1,
		FirmwareComplete = 2,
		FirmwareFail = 3,
		FirmwareUpToDate = 4,
		FirmwareOutOfDate = 5,
		BluetoothStartDiscoverable = 6,
		BluetoothEndDiscoverable = 7
	}

	public enum PutBytesType : byte
	{
		Init=0x01,
		Put=0x02,
		Commit=0x03,
		Abort=0x04,
		Install=0x05,
	}
}