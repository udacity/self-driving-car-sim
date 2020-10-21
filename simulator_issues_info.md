## Known Simulator Issues

- **Log File Issue:** Students have reported rapid expansion of log files when using the term 2 and term 3 simulators, which can cause issues with system memory.  We plan to address this in a future release. This appears to be associated with not being connected to uWebSockets. If this does occur, please make sure you are conneted to uWebSockets. The following workaround may also be effective at preventing large log files.  For linux users, log file behavior can be adjusted through the Unity editor via ```player settings```.  The strategy below has proven effective for some students, with non-Linux OS.
  + create an empty log file
  + remove write permissions so that the simulator can't write to log

- **Locale Issue:**  Some students have reported term 2 simulator issues related to non-US locale settings.  We plan to address this in a future release.  More information can be found [here](https://discussions.udacity.com/t/datasets-seem-to-be-missing-in-simulator-v1-45-ubuntu-16-04/373597/23?u=subodh.malgonde) and [here](https://discussions.udacity.com/t/term-2-simulator-not-working-properly/446386/5?u=subodh.malgonde).  The following workarounds have been succesful.
  -  **Workaround 1:**
    + ```sudo locale-gen "en_US.UTF-8"```
    + ```sudo dpkg-reconfigure locales``` Choose en-us
    + Restart
- **Workaround 2:**
  + Change Regional Formats (Ubuntu: Language Support -> Regional Formats)
  + Select English (US)
  + Restart
