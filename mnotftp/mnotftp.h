#pragma once
#include <string>

#define DLL_MACRO __declspec(dllexport)
#ifndef DLL_MACRO
#ifdef BUILDING_DLL
#define DLL_MACRO __declspec(dllexport)
#else
#define DLL_MACRO __declspec(dllimport)
#endif
#endif


namespace Cpp {
	typedef void(__stdcall *UpdateCallback)(char *);

	class TftpMT {
	public:
		static void DLL_MACRO nRun(UpdateCallback callback);
	};
}

