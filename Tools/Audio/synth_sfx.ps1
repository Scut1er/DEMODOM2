# Синтез простых (не голосовых) звуков через ffmpeg.
# Запуск из корня проекта:  powershell -ExecutionPolicy Bypass -File Tools/Audio/synth_sfx.ps1
# Результат: Assets/Resources/Sfx/<имя>.ogg (моно, 44.1 kHz, пик -1 dBFS). Права — наши, без атрибуции.
# Чтобы подправить звук: поменяй формулу/фильтр в таблице ниже и перезапусти.

# ffmpeg пишет служебные сообщения в stderr — в PowerShell 5.1 со "Stop" это ломает скрипт.
$ErrorActionPreference = "Continue"
$env:PATH = "$env:LOCALAPPDATA\Microsoft\WinGet\Links;$env:PATH"
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$out = Join-Path $root "Assets/Resources/Sfx"
$tmp = Join-Path $env:TEMP "synth_sfx"
New-Item -ItemType Directory -Force $out, $tmp | Out-Null

# Металлический удар для труб: несколько негармоничных частот с затуханием.
function Metal([string]$tt) {
    "exp(-10*($tt))*(sin(2*PI*410*($tt))+0.6*sin(2*PI*1093*($tt))+0.4*sin(2*PI*1867*($tt))+0.25*sin(2*PI*2690*($tt)))"
}
$pipeHits = @(@(0, 1.0), @(0.32, 0.7), @(0.5, 0.5), @(1.05, 0.9))
$pipes = ($pipeHits | ForEach-Object { "gte(t,$($_[0]))*$($_[1])*" + (Metal "t-$($_[0])") }) -join "+"
$pipes = "0.3*($pipes)+0.08*(random(0)*2-1)*(0.5+0.5*sin(2*PI*9*t))*gt(t,0.2)"

$sounds = [ordered]@{
    # «!» — NPC что-то заметил: два коротких восходящих пинга
    alert = @{ d = 0.32; f = "aecho=0.6:0.4:40:0.25,afade=t=out:st=0.25:d=0.07"
        e = "0.6*sin(2*PI*if(lt(t,0.09),988,1480)*t)*if(lt(t,0.09),exp(-20*t),exp(-12*(t-0.09)))" }

    # появление отзыва — мягкий «поп»-пузырёк
    notify = @{ d = 0.18; f = "anull"
        e = "0.8*sin(2*PI*(320*t+600/28*(1-exp(-28*t))))*exp(-26*t)*(1-exp(-400*t))" }

    # апгрейд команды — арпеджио до-ми-соль-до
    levelup = @{ d = 0.9; f = "aecho=0.7:0.5:60|120:0.3|0.2"
        e = "st(0,if(lt(t,0.09),523.25,if(lt(t,0.18),659.25,if(lt(t,0.27),783.99,1046.5))));st(1,if(lt(t,0.27),mod(t,0.09),t-0.27));st(2,if(lt(t,0.27),exp(-10*ld(1)),exp(-3.5*ld(1))));0.45*ld(2)*(1-exp(-300*ld(1)))*(sin(2*PI*ld(0)*ld(1))+0.3*sin(4*PI*ld(0)*ld(1))+0.15*sin(6*PI*ld(0)*ld(1)))" }

    # пожарная сигнализация — три писка дымового датчика (~3.1 kHz)
    fire_alarm = @{ d = 2.05; f = "highpass=f=500"
        e = "0.45*lt(mod(t,0.75),0.5)*(1-exp(-300*mod(t,0.75)))*(sin(2*PI*3150*t)+0.25*sin(2*PI*6300*t))" }

    # «Нет сети» — понижающийся «power down» + два низких сигнала ошибки
    wifi_off = @{ d = 0.95; f = "lowpass=f=5000,afade=t=in:d=0.005"
        e = "st(0,1000*t-750*t*t);st(1,if(lt(t,0.55),0.5*exp(-1.5*t)*(sin(2*PI*ld(0))+0.3*sin(6*PI*ld(0))),0));st(2,between(t,0.6,0.72)+between(t,0.78,0.92));ld(1)+0.35*ld(2)*(sin(2*PI*196*t)+0.33*sin(6*PI*196*t)+0.2*sin(10*PI*196*t))" }

    # режим камеры — бип записи + жужжание сервопривода зума
    cam_on = @{ d = 0.75; f = "lowpass=f=3500"
        e = "st(0,0.5*between(t,0,0.07)*sin(2*PI*2000*t));st(1,between(t,0.12,0.7)*0.28*sin(PI*(t-0.12)/0.58)*(0.8+0.2*sin(2*PI*31*t)));st(2,150*t+45*t*t);ld(0)+ld(1)*(2*mod(ld(2),1)-1)" }

    # хлопушка «Мотор!/Снято!» — резкий деревянный щелчок
    clapper = @{ d = 0.3; f = "highpass=f=150,aecho=0.5:0.3:25:0.3"
        e = "0.9*(random(0)*2-1)*exp(-70*t)+0.5*sin(2*PI*210*t)*exp(-45*t)" }

    # печать «утверждено» на карте — глухой удар + шлепок бумаги
    stamp = @{ d = 0.35; f = "lowpass=f=2500"
        e = "st(0,90*t+60/30*(1-exp(-30*t)));0.9*sin(2*PI*ld(0))*exp(-16*t)+0.35*(random(0)*2-1)*exp(-90*t)" }

    # возгорание холодильника — «пых» + низкий бум
    fridge_pop = @{ d = 1.0; f = "lowpass=f=1800,highpass=f=40"
        e = "0.6*(random(0)*2-1)*(1-exp(-35*t))*exp(-4.5*t)+0.7*sin(2*PI*55*t)*exp(-7*t)*(1-exp(-200*t))" }

    # мухи над тухлятиной — жужжание с «облётом» (можно зациклить)
    flies = @{ d = 3.0; f = "highpass=f=180,lowpass=f=3000,afade=t=in:d=0.3,afade=t=out:st=2.6:d=0.4"
        e = "st(0,215*t-(16/(12*PI))*cos(12*PI*t)-(9/(2.6*PI))*cos(2.6*PI*t));0.32*(0.65+0.35*sin(2*PI*0.7*t))*(2*mod(ld(0),1)-1)" }

    # «дун-дун-ДУН» — драматичный стингер (два удара + долгий низкий с литаврой)
    sting = @{ d = 2.2; f = "lowpass=f=2200,aecho=0.8:0.6:90|180:0.35|0.25"
        e = "st(0,if(lt(t,0.6),146.83,110));st(1,if(lt(t,0.3),t,if(lt(t,0.6),t-0.3,t-0.6)));st(2,if(lt(t,0.6),exp(-7*ld(1)),exp(-1.6*ld(1))));st(3,2*PI*ld(0)*ld(1));0.22*ld(2)*(1-exp(-120*ld(1)))*(sin(ld(3))+0.5*sin(2*ld(3))+0.33*sin(3*ld(3))+0.25*sin(4*ld(3))+0.45*sin(1.5*ld(3))+0.6*sin(0.5*ld(3)))+0.5*gt(t,0.6)*sin(2*PI*55*(t-0.6))*exp(-3*(t-0.6))" }

    # раскрытие скрытой черты — нарастающий «взлёт» + яркий аккорд
    reveal = @{ d = 1.6; f = "aecho=0.8:0.7:70|140:0.4|0.3"
        e = "st(0,300*t+450*t*t);st(1,t-0.85);lt(t,0.85)*0.2*(t/0.85)*(sin(2*PI*ld(0))+0.5*sin(3*PI*ld(0)))*(0.7+0.3*sin(2*PI*12*t))+gte(t,0.85)*0.17*exp(-2.5*ld(1))*(1-exp(-200*ld(1)))*(sin(2*PI*523.25*ld(1))+sin(2*PI*659.25*ld(1))+sin(2*PI*783.99*ld(1))+0.7*sin(2*PI*1046.5*ld(1)))" }

    # «Нет воды» — трубы стучат и булькают
    pipes = @{ d = 1.8; f = "lowpass=f=6000"; e = $pipes }
}

foreach ($name in $sounds.Keys) {
    $s = $sounds[$name]
    $wav = Join-Path $tmp "$name.wav"
    $ogg = Join-Path $out "$name.ogg"
    & ffmpeg -hide_banner -loglevel error -y -f lavfi -i "aevalsrc='$($s.e)':s=44100:d=$($s.d)" -af $s.f -ac 1 $wav
    if ($LASTEXITCODE -ne 0) { Write-Warning "$name failed"; continue }
    # пиковая нормализация до -1 dBFS
    $peak = (& ffmpeg -hide_banner -i $wav -af volumedetect -f null NUL 2>&1 | Select-String "max_volume: (-?[\d.]+) dB").Matches.Groups[1].Value
    $gain = -1.0 - [double]$peak
    & ffmpeg -hide_banner -loglevel error -y -i $wav -af "volume=${gain}dB" -ac 1 -ar 44100 -c:a libvorbis -q:a 6 $ogg
    "{0,-11} {1,5:N2}s  peak {2,6} dB -> {3:+0.0;-0.0} dB" -f $name, $s.d, $peak, $gain
}
