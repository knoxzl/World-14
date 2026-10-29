cmd-planet14_status-desc = Shows the Basirath sector clock and scheduled evacuation phase.
cmd-planet14_status-help = Usage: planet14_status
cmd-planet14_start-desc = Starts and saves the Planet 14 sector clock. Requires planet14.enabled at server startup.
cmd-planet14_start-help = Usage: planet14_start

planet14-clock-disabled = The Planet 14 clock is disabled. Set planet14.enabled before starting the server.
planet14-clock-load-failed = The saved sector clock could not be loaded. Check the server log and repair the save before starting a cycle.
planet14-clock-already-started = A sector cycle already exists. It cannot be restarted with this command.
planet14-clock-started = The Basirath sector clock has started and been saved.
planet14-clock-start-failed = The sector clock could not be saved or its day length is invalid. Check the server log. No cycle was started.
planet14-clock-unavailable = No sector clock is running. The clock may be disabled, not yet started, or its save could not be loaded.
planet14-clock-status = Basirath — day {$day}/15. Black Space: {$blackSpace}. Scheduled phase: {$phase}. Collapse in {$remaining}.

planet14-blackspace-unstable = unstable; entry is fatal
planet14-blackspace-traversable = traversable; return is possible
planet14-blackspace-oneway = one-way; return is closed
planet14-blackspace-consumed = sector consumed

planet14-phase-survival = survival
planet14-phase-capacityreview = evacuation capacity review
planet14-phase-shieldcharging = evacuation shield charging
planet14-phase-boarding = boarding
planet14-phase-rush = final boarding rush
planet14-phase-finalfiveminutes = final five minutes
planet14-phase-departed = evacuation departed
planet14-phase-lastday = final day
planet14-phase-collapsed = sector collapsed
