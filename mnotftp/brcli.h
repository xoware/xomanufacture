#pragma once
using namespace System;
using namespace System::Collections;
using namespace System::Diagnostics;
using namespace System::Runtime::InteropServices;

#include "mnotftp.h"

namespace BRCLI {

	public delegate void BridgeCall(IntPtr);

	/// <summary>
	/// Summary for BridgeC
	/// </summary>
	public ref class BridgeC  abstract
	{

	public:
		static void RunCLI(BridgeCall^ _bcall)
		{
			static GCHandle gch;
			gch = GCHandle::Alloc(_bcall);

			IntPtr cbPtr = Marshal::GetFunctionPointerForDelegate(_bcall);
			Cpp::TftpMT::nRun(static_cast<Cpp::UpdateCallback>(cbPtr.ToPointer()));

			gch.Free();
		}
	};
}


