implement main
    open core, std

domains
    clickRecord = click(real X, real Y).
    voiceRecord = voice(real SinParam, real CotangParam).
    % Changed: used a distinct functor "face3dmat" and placed it here cleanly
    face3dmat = face3d(real X, real Y, real Z).

class facts
    patient_static_click : (string PatientId, clickRecord ClickData) determ.
    patient_static_voice : (string PatientId, voiceRecord VoiceData) determ.
    internal_counter : integer := erroneous.
    current_state : string := erroneous.

class predicates
    combinational_logic : (string PatientId, face3dmat LiveFace, integer Score) -> integer Code.
    stream_loop : ().

clauses
    run() :-
        assert(patient_static_click("p1", click(100.0, 100.0))),
        assert(patient_static_voice("p1", voice(0.5, 0.0))),
        stream_loop.

    stream_loop() :-
        InputStr = stdio::readLine(),
        InputStr <> "",
        !,
        if Pos1 = string::search(InputStr, " ") and Id = string::subString(InputStr, 0, Pos1) and
            Rest1 = string::subString(InputStr, Pos1 + 1, string::length(InputStr) - Pos1 - 1) and Pos2 = string::search(Rest1, " ") and
            XStr = string::subString(Rest1, 0, Pos2) and Rest2 = string::subString(Rest1, Pos2 + 1, string::length(Rest1) - Pos2 - 1) and
            Pos3 = string::search(Rest2, " ") and YStr = string::subString(Rest2, 0, Pos3) and
            Rest3 = string::subString(Rest2, Pos3 + 1, string::length(Rest2) - Pos3 - 1) and Pos4 = string::search(Rest3, " ") and
            ZStr = string::subString(Rest3, 0, Pos4) and ScoreStr = string::subString(Rest3, Pos4 + 1, string::length(Rest3) - Pos4 - 1)
        then
            LiveX = toTerm(real, XStr),
            LiveY = toTerm(real, YStr),
            LiveZ = toTerm(real, ZStr),
            Score = toTerm(integer, ScoreStr),
            % Вызываем логику и пишем в C#
            ResultCode = combinational_logic(Id, face3d(LiveX, LiveY, LiveZ), Score),
            stdio::writef("%d\n", ResultCode)
        end if,
        stream_loop().
    stream_loop().

    combinational_logic(PatientId, face3d(LiveX, LiveY, LiveZ), _CLK) = OutputCode :-
        if patient_static_click(PatientId, click(StatX, StatY)) and patient_static_voice(PatientId, voice(_SinP, CotP)) then
            if CotP > 0.8 and math::abs(LiveX - StatX) > 50.0 and LiveZ < 0.1 then
                internal_counter := internal_counter + 1,
                if internal_counter > 5 then
                    current_state := "anomaly_state"
                end if,
                OutputCode = 2
            else
                OutputCode = 1
            end if
        else
            OutputCode = 0
        end if.

end implement main

goal
    main::run.
