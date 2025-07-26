declare let luxon: any;
declare let YT: any;

class Util {
    static MutationObservers = [] as MutationObserver[];
    public static findATagByInnerText(text: string) {
        const allATags = document.querySelectorAll("a");
        for (let i = 0; i < allATags.length; i++) {
            if (allATags[i].innerText.trim() === text.trim()) {
                return allATags[i];
            }
        }
        return null; // Not found
    }

    static toggleAc() {

        const url = 'http://10.0.0.79:8222/json'; // Replace <ESP32-IP> with your ESP32's IP address

        // JSON data to be sent in the request
        const data = {
            action: "hitSwitch",
        };

        console.log("start toggleAc");
        fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                return response.json();
            })
            .then(jsonResponse => {
                console.log("response toggleAc");
                console.log('Response:', jsonResponse);
            })
            .catch(error => {
                console.error('Error:', error);
            });



    }

    public static clickATagByInnerText(text: string) {
        const allATags = document.querySelectorAll("a");
        for (let i = 0; i < allATags.length; i++) {
            if (allATags[i].innerText.trim() === text.trim()) {
                allATags[i].click();
            }
        }
    }

    public static async SendData(data: any) {

        let image = document.querySelector("mini-monitor-img") as HTMLImageElement;
        if (image) {
            image.parentElement?.removeChild(image);
        }

        console.debug("SendData:", JSON.stringify(data, null, 5));

        const url = 'http://127.0.0.1:9191/mini-monitor';
        image = document.createElement("img") as HTMLImageElement;
        image.style.display = "none";
        image.src = url + `?data=${encodeURIComponent(JSON.stringify(data))}`
        image.classList.add("mini-monitor-img")
        document.body.appendChild(image);
    }


    public static GetMailboxInfo(): any {

        let unread = 0;
        let read = 0;
        let latestUnread = null as any;

        let parseTimeString = function (timeString: string): Date | null {
            // Split the time string into hours, minutes, and AM/PM components
            try {

                if (timeString.indexOf("AM") >= 0 || timeString.indexOf("PM") >= 0 && (timeString.length === 7 || timeString.length === 8)) {

                    timeString = timeString.replace(" ", ":");

                    const [hours, minutes, meridian] = timeString.split(':').map(part => part.trim());

                    // Convert hours to 24-hour format based on AM/PM
                    const adjustedHours = meridian === 'PM' ? (parseInt(hours, 10) + 12) % 24 : +hours;

                    // Create a Date object with provided time and set year, month, and day to 1
                    let date = new Date();

                    date.setHours(adjustedHours);
                    date.setMinutes(+minutes);

                    return date;
                }

            } catch (ex) {


            }

            return null;

        }

        //
        document.querySelectorAll(`div:not([attributeName="data-convid"]`).forEach(x => {
            let label = x.getAttribute("aria-label");

            if (label && label.startsWith("Unread")) {
                unread++;
                if (latestUnread === null) {
                    latestUnread = {
                        sender: x.querySelector(".mn28d.XG5Jd > div.JBWmn.gy2aJ.Ejrkd > span")?.innerHTML.trim(),
                        subject: x.querySelector(".ovvvr > div> span")?.innerHTML.trim(),
                        receivedOn: parseTimeString(x.querySelector(".WP8_u > div.lulAg > span")?.innerHTML.trim() as string),
                    };
                }
            } else {
                read++;
            }

        });

        return JSON.stringify({ unread: unread, read: read, latestUnread: latestUnread }, null, 5);




    }

    static GetMutationObserver(fn: (mutationList: MutationRecord[], observer: MutationObserver) => void): MutationObserver {
        let mo = new MutationObserver(fn);
        Util.MutationObservers.push(mo);
        return mo;
    }


    public static WatchCurrentSong() {



        let songTag = document.querySelector("#layout > ytmusic-player-bar > div.middle-controls.style-scope.ytmusic-player-bar > div.content-info-wrapper.style-scope.ytmusic-player-bar > yt-formatted-string");

        if (songTag != null) {

            let fn = function (ele: HTMLElement) {
                let result = { dataType: "GetCurrentSong", found: false } as any;
                let artist = ele.parentElement?.querySelector("span > span.subtitle.style-scope.ytmusic-player-bar > yt-formatted-string > a:nth-child(1)") as HTMLElement;

                if (ele.innerText) {
                    result.found = true;
                    result.title = ele.innerText;
                    result.artist = artist.innerText;
                }

                let data =
                {
                    DataType: "MusicUpdate",
                    Success: true,
                    Data: result
                };


                Util.SendData(data);
            }


            if (!songTag.getAttribute("mini-mon-watched")) {

                let options = {
                    childList: false,
                    subtree: false,
                    attributes: true,
                    characterData: true
                };

                let observer = Util.GetMutationObserver(function (mutations) {
                    for (let mutation of mutations) {
                        let changedElement = mutation.target as HTMLElement
                        fn(changedElement);
                    }
                })

                observer.observe(songTag, options);
                songTag.setAttribute("mini-mon-watched", "true");
            }

        }



    }


    public static GetCurrentSong(): string {
        let result = { dataType: "GetCurrentSong", found: false } as any;
        let song = document.querySelector("#layout > ytmusic-player-bar > div.middle-controls.style-scope.ytmusic-player-bar > div.content-info-wrapper.style-scope.ytmusic-player-bar > yt-formatted-string") as HTMLDivElement;
        let artist = document.querySelector("#layout > ytmusic-player-bar > div.middle-controls.style-scope.ytmusic-player-bar > div.content-info-wrapper.style-scope.ytmusic-player-bar > span > span.subtitle.style-scope.ytmusic-player-bar > yt-formatted-string > a:nth-child(1)") as HTMLAnchorElement;

        if (song != null && artist != null) {
            result.found = true;
            result.title = song.innerText;
            result.artist = artist.innerText;
        }


        return JSON.stringify(result);
    }
}

class MyClass {

    static ytm: Window | null = null;
    static external = window["external"] as any;
    static lastSoundTime = new Date("2000/01/01");

    //
    static trySetInnerHTML(id: string, text: string | null | undefined) {

        if (text === null || text === undefined) {
            text = "";
        };

        let x = document.getElementById(id) as HTMLHtmlElement;
        if (x) {
            x.innerHTML = text;
        }
    }
    static trySetInnerText(id: string, text: string | null | undefined) {

        if (text === null || text === undefined) {
            text = "";
        };

        let x = document.getElementById(id) as HTMLHtmlElement;
        if (x) {
            x.innerText = text;
        }
    }

    //HandleMessage

    static lastCalendarData = null;
    static lastCalendarInterval = 0;
    static blinkIntervalRef = 0;
    static lastBlinkInterval = 0;




    public static ToggleDeskLight() {
        $.ajax({
            url: `http://reartvlight.local/doAction`,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ "action": "light_toggle" }),
            success: function (jsonResponse) {
                console.log(`done`, jsonResponse);
            },
            error: function (error) {
                console.error('Error:', error);
            }
        });
    }

    public static ToggleDisplayArea() {
        let me = MyClass;
        let displays = ["dvPrimary", "dvSecondary"];

        if ($($(".dvPrimary")[0]).is(":visible")) {
            $(".dvPrimary").hide();
            $(".dvSecondary").show();
        } else {
            $(".dvPrimary").show();
            $(".dvSecondary").hide();
        }
    }

    public static BlinkText(elementId: string, blinkInterval: number) {
        let me = this;
        const element = document.getElementById(elementId) as HTMLHtmlElement;
        let visible = true;

        if (me.blinkIntervalRef) {
            clearInterval(me.blinkIntervalRef);
        }


        me.blinkIntervalRef = setInterval(() => {
            visible = !visible;


            if (blinkInterval >= 2000) {
                if (visible) {
                    me.BlinkTextTimes(elementId, !visible, 4);
                } else {
                    me.BlinkTextTimes(elementId, visible, 4);
                }


            } else {
                // just toggle
                element.style.opacity = visible ? "1" : "0";
            }


        }, blinkInterval);
    }

    public static BlinkTextTimes(elementId: string, visible: boolean, count: number) {
        let me = this;
        const element = document.getElementById(elementId) as HTMLHtmlElement;

        visible = !visible;

        element.style.opacity = visible ? "1" : "0";

        if (count > 0) {
            setTimeout(function () {
                me.BlinkTextTimes(elementId, visible, count - 1);
            }, 200);
        }

    }


    public static HandleMessage(data: string) {
        let me = this;

        let dataObject = JSON.parse(data);


        if (dataObject.DataType === "MusicUpdate") {

            if (dataObject.Success && dataObject.Data.found === true) {
                this.trySetInnerText("playingSong", dataObject.Data.title);
                this.trySetInnerText("playingArtist", dataObject.Data.artist);

            } else {
                this.trySetInnerText("playingSong", "Nothing playing...");
                this.trySetInnerText("playingArtist", "");

            }



        }

        if (dataObject.DataType === "CalendarData") {


            me.lastCalendarData = dataObject;


            if (me.lastCalendarInterval) {
                clearInterval(me.lastCalendarInterval);
            }

            var updateCalData = function () {

                if (dataObject.HasEvents === false) {
                    MyClass.setStyleDisplay("nextMeeting", "none");
                    return;
                }


                me.trySetInnerText("meetingTitle", dataObject.Summary);
                let timeMessage = "";
                let minutesUntil = (new Date(dataObject.StartTimeUtc as string).getTime() - new Date().getTime()) / 1000.0 / 60.0;

                let formattedRel = (luxon.DateTime.fromISO(dataObject.StartTimeUtc)).toRelative({ base: luxon.DateTime.now(), style: 'long' });
                if (minutesUntil > 0) {
                    timeMessage = `${formattedRel}`;
                } else {
                    timeMessage = `started ${formattedRel}`;
                }


                me.trySetInnerText("timeUntilMeeting", timeMessage);
                //me.trySetInnerText("timeUntilMeeting", (minutesUntil).toString());

                document.getElementById("nextMeeting")?.classList.remove("nextMeetingDueSoon");
                document.getElementById("nextMeeting")?.classList.remove("nextMeetingOverDue");
                document.getElementById("nextMeeting")?.classList.remove("nextMeetingInProgress");

                if (minutesUntil <= 0) {
                    document.getElementById("nextMeeting")?.classList.add("nextMeetingInProgress");
                } else if (minutesUntil <= 2) {
                    document.getElementById("nextMeeting")?.classList.add("nextMeetingOverDue");
                } else if (minutesUntil <= 15) {
                    document.getElementById("nextMeeting")?.classList.add("nextMeetingDueSoon");
                }


                let soundIntervalMinutes = 0;
                let blinkIntervalMs = 0;
                if (minutesUntil < 1) {
                    blinkIntervalMs = 500;
                    soundIntervalMinutes = 60;
                } else if (minutesUntil < 2) {
                    blinkIntervalMs = 500;
                    soundIntervalMinutes = 60;
                } else if (minutesUntil < 5) {
                    blinkIntervalMs = 1000;
                    soundIntervalMinutes = 120;
                } else if (minutesUntil < 15) {
                    blinkIntervalMs = 2000;
                    soundIntervalMinutes = 120;
                } else if (minutesUntil < 30) {
                    soundIntervalMinutes = 300;
                    blinkIntervalMs = 3000;
                } else if (minutesUntil < 60) {
                    blinkIntervalMs = 5000;
                }

                if (blinkIntervalMs === 0) {
                    clearInterval(me.blinkIntervalRef);
                } else {
                    if (me.lastBlinkInterval !== blinkIntervalMs) {
                        me.lastBlinkInterval = blinkIntervalMs;
                        me.BlinkText('nextMeeting', blinkIntervalMs);
                    }
                }


                if (soundIntervalMinutes > 0 && minutesUntil > 0) {
                    // only if there is an interval AND the meeting is in the future.
                    let timeDiffSeconds = (new Date().getTime() - me.lastSoundTime.getTime()) / 1000
                    if (timeDiffSeconds > soundIntervalMinutes) {
                        me.lastSoundTime = new Date();
                        const audioElement = new Audio('assets/Alarm04.wav');
                        audioElement.play();
                    }
                }


                MyClass.setStyleDisplay("nextMeeting", "block");
            }


            me.lastCalendarInterval = setInterval(updateCalData, 1000);


        } else if (dataObject.DataType === "SensorData") {
            me.trySetInnerText("spCpu", (+dataObject.cpuTotal).toFixed(1).padStart(4, '0'));
            //me.trySetInnerText("spCpu", Math.round(+dataObject.cpuTotal).toString().padStart(2, '0'));

            me.trySetInnerText("spGpu", (+dataObject.gpuLoad).toFixed(1).padStart(4, '0'));
            //me.trySetInnerText("spGpu", Math.round(+dataObject.gpuLoad).toString().padStart(2, '0'));
        } else if (dataObject.DataType === "WeatherData") {
            me.trySetInnerText("tempDisplay", `, ${dataObject.Temperature}°`);
        }


        //console.log(`data: ${data}!`)
    }

    private static setStyleDisplay(id: string, value: string) {

        let ele = document.getElementById(id);
        if (ele != null) {
            ele.style.display = value;

            if (value === "block") {
                ele.style.opacity = "1";
            }
        }
    }

    public static isPageB() {

        if (window.location.origin === "file://") {
            return window.location.pathname.indexOf("index-b.html") >= 0;
        }

        return false;
    }


    public static weather_conditions =
        [
            { "code": 200, "short_name": "Thunderstorm", "description": "thunderstorm with light rain", "icon": "11d" },
            { "code": 201, "short_name": "Thunderstorm", "description": "thunderstorm with rain", "icon": "11d" },
            { "code": 202, "short_name": "Thunderstorm", "description": "thunderstorm with heavy rain", "icon": "11d" },
            { "code": 210, "short_name": "Thunderstorm", "description": "light thunderstorm", "icon": "11d" },
            { "code": 211, "short_name": "Thunderstorm", "description": "thunderstorm", "icon": "11d" },
            { "code": 212, "short_name": "Thunderstorm", "description": "heavy thunderstorm", "icon": "11d" },
            { "code": 221, "short_name": "Thunderstorm", "description": "ragged thunderstorm", "icon": "11d" },
            { "code": 230, "short_name": "Thunderstorm", "description": "thunderstorm with light drizzle", "icon": "11d" },
            { "code": 231, "short_name": "Thunderstorm", "description": "thunderstorm with drizzle", "icon": "11d" },
            { "code": 232, "short_name": "Thunderstorm", "description": "thunderstorm with heavy drizzle", "icon": "11d" },
            { "code": 300, "short_name": "Drizzle", "description": "light intensity drizzle", "icon": "09d" },
            { "code": 301, "short_name": "Drizzle", "description": "drizzle", "icon": "09d" },
            { "code": 302, "short_name": "Drizzle", "description": "heavy intensity drizzle", "icon": "09d" },
            { "code": 310, "short_name": "Drizzle", "description": "light intensity drizzle rain", "icon": "09d" },
            { "code": 311, "short_name": "Drizzle", "description": "drizzle rain", "icon": "09d" },
            { "code": 312, "short_name": "Drizzle", "description": "heavy intensity drizzle rain", "icon": "09d" },
            { "code": 313, "short_name": "Drizzle", "description": "shower rain and drizzle", "icon": "09d" },
            { "code": 314, "short_name": "Drizzle", "description": "heavy shower rain and drizzle", "icon": "09d" },
            { "code": 321, "short_name": "Drizzle", "description": "shower drizzle", "icon": "09d" },
            { "code": 500, "short_name": "Rain", "description": "light rain", "icon": "10d" },
            { "code": 501, "short_name": "Rain", "description": "moderate rain", "icon": "10d" },
            { "code": 502, "short_name": "Rain", "description": "heavy intensity rain", "icon": "10d" },
            { "code": 503, "short_name": "Rain", "description": "very heavy rain", "icon": "10d" },
            { "code": 504, "short_name": "Rain", "description": "extreme rain", "icon": "10d" },
            { "code": 511, "short_name": "Rain", "description": "freezing rain", "icon": "13d" },
            { "code": 520, "short_name": "Rain", "description": "light intensity shower rain", "icon": "09d" },
            { "code": 521, "short_name": "Rain", "description": "shower rain", "icon": "09d" },
            { "code": 522, "short_name": "Rain", "description": "heavy intensity shower rain", "icon": "09d" },
            { "code": 531, "short_name": "Rain", "description": "ragged shower rain", "icon": "09d" },
            { "code": 600, "short_name": "Snow", "description": "light snow", "icon": "13d" },
            { "code": 601, "short_name": "Snow", "description": "snow", "icon": "13d" },
            { "code": 602, "short_name": "Snow", "description": "heavy snow", "icon": "13d" },
            { "code": 611, "short_name": "Snow", "description": "sleet", "icon": "13d" },
            { "code": 612, "short_name": "Snow", "description": "light shower sleet", "icon": "13d" },
            { "code": 613, "short_name": "Snow", "description": "shower sleet", "icon": "13d" },
            { "code": 615, "short_name": "Snow", "description": "light rain and snow", "icon": "13d" },
            { "code": 616, "short_name": "Snow", "description": "rain and snow", "icon": "13d" },
            { "code": 620, "short_name": "Snow", "description": "light shower snow", "icon": "13d" },
            { "code": 621, "short_name": "Snow", "description": "shower snow", "icon": "13d" },
            { "code": 622, "short_name": "Snow", "description": "heavy shower snow", "icon": "13d" },
            { "code": 701, "short_name": "Mist", "description": "mist", "icon": "50d" },
            { "code": 711, "short_name": "Smoke", "description": "smoke", "icon": "50d" },
            { "code": 721, "short_name": "Haze", "description": "haze", "icon": "50d" },
            { "code": 731, "short_name": "Dust", "description": "sand/dust whirls", "icon": "50d" },
            { "code": 741, "short_name": "Fog", "description": "fog", "icon": "50d" },
            { "code": 751, "short_name": "Sand", "description": "sand", "icon": "50d" },
            { "code": 761, "short_name": "Dust", "description": "dust", "icon": "50d" },
            { "code": 762, "short_name": "Ash", "description": "volcanic ash", "icon": "50d" },
            { "code": 771, "short_name": "Squall", "description": "squalls", "icon": "50d" },
            { "code": 781, "short_name": "Tornado", "description": "tornado", "icon": "50d" },
            { "code": 801, "short_name": "Clouds", "description": "few clouds: 11-25%", "icon": "02d" },
            { "code": 802, "short_name": "Clouds", "description": "scattered clouds: 25-50%", "icon": "03d" },
            { "code": 803, "short_name": "Clouds", "description": "broken clouds: 51-84%", "icon": "04d" },
            { "code": 804, "short_name": "Clouds", "description": "overcast clouds: 85-100%", "icon": "04d" },
            { "code": 805, "short_name": "Clouds", "description": "overcast clouds", "icon": "04d" },
            { "code": 806, "short_name": "Clear Sky", "description": "clear sky", "icon": "01d" },

        ];


    public static findYouTubePlayerObject(): any {
        const visited = new WeakSet(); // Use WeakSet to avoid strong references and memory leaks for objects

        function traverse(obj: any): any {
            if (obj === null || typeof obj !== 'object' || visited.has(obj)) {
                return null; // Skip null, primitives, and already visited objects
            }

            visited.add(obj);

            try {
                // Check for both methods on the current object
                if (
                    typeof obj.playVideo === 'function' &&
                    typeof obj.getVideoData === 'function'
                ) {
                    return obj; // Found the object, return it immediately
                }

                // Iterate over properties and recurse
                const properties = Object.getOwnPropertyNames(obj).concat(Object.keys(obj)); // Get own and enumerable properties

                for (const prop of properties) {
                    // Avoid common problematic properties to prevent errors or infinite loops
                    // This list can be adjusted based on your needs.
                    if (
                        prop === 'window' ||
                        prop === 'document' ||
                        prop === 'self' ||
                        prop === 'top' ||
                        prop === 'parent' ||
                        prop === 'frames' ||
                        prop === 'history' ||
                        prop === 'location' ||
                        prop === 'navigator' ||
                        prop === 'screen' ||
                        prop === 'performance' ||
                        prop === 'console' ||
                        prop === 'localStorage' ||
                        prop === 'sessionStorage' ||
                        prop === 'globalThis' ||
                        prop === '__proto__' // Avoid direct __proto__ access for robustness
                    ) {
                        continue;
                    }

                    let value;
                    try {
                        value = obj[prop];
                    } catch (e) {
                        // Some properties might throw errors on access
                        continue;
                    }

                    // Only traverse if it's an object and not null (and not a function itself)
                    if (value !== null && typeof value === 'object') {
                        const found = traverse(value);
                        if (found) {
                            return found; // If found in a nested object, pass it up the chain
                        }
                    }
                }
            } catch (e) {
                // Catch potential errors during property access (e.g., security errors)
                console.warn(`Error traversing object:`, obj, e);
            }
            return null; // Not found in this branch
        }

        return traverse(window); // Start the traversal from the window object
    }


    public static clickButton(buttonElement: HTMLButtonElement): boolean {
        // --- Input Validation ---
        if (!buttonElement) {
            console.error("Error: The 'buttonElement' provided is null or undefined. Cannot dispatch click event.");
            // Return false or throw an error based on desired error handling
            throw new Error("Invalid input: buttonElement is null or undefined.");
        }
        if (!(buttonElement instanceof HTMLButtonElement)) {
            console.error(`Error: Expected an HTMLButtonElement, but received a ${buttonElement.constructor.name}.`);
            throw new Error("Invalid input: The provided element is not an HTMLButtonElement.");
        }

        try {
            // --- Create the MouseEvent ---
            // A MouseEvent is used for clicks.
            // 'bubbles: true' allows the event to propagate up the DOM tree,
            // mimicking natural event behavior.
            // 'cancelable: true' allows the event to be prevented (e.g., if a listener calls event.preventDefault()).
            // 'view: window' links the event to the current window context.
            const clickEvent = new MouseEvent('click', {
                bubbles: true,
                cancelable: true,
                view: window
            });

            // --- Dispatch the Event ---
            // dispatchEvent triggers the event on the specified element.
            // It returns false if any event listener called event.preventDefault().
            const wasDispatchedSuccessfully = buttonElement.dispatchEvent(clickEvent);

            if (wasDispatchedSuccessfully) {
                console.log(`Successfully dispatched a synthetic click event to button with ID: ${buttonElement.id || 'N/A'}`);
            } else {
                console.warn(`Synthetic click event on button with ID: ${buttonElement.id || 'N/A'} was canceled by a listener.`);
            }

            return wasDispatchedSuccessfully;

        } catch (error) {
            console.error(`An error occurred while attempting to click the button with ID: ${buttonElement.id || 'N/A'}.`, error);
            return false; // Indicate failure
        }
    }


    public static handleMusicControlEvent(event: MessageEvent) {
        let me = MyClass;
        console.log('Message from server:', event.data);

        let eventData = JSON.parse(event.data);

        if (eventData.action === "pause") {
            me.findYouTubePlayerObject().pauseVideo();
        } else if (eventData.action === "next") {
            me.clickButton(document.querySelector("div.ytmusic-player-bar button.yt-icon-button[aria-label='Next']") as HTMLButtonElement);
        } else {
            me.findYouTubePlayerObject().playVideo();
        }

        setTimeout(async function () {
            await me.sendMusicData();
            setTimeout(async function () {
                await me.sendMusicData();
            }, 500);
        }, 10);
    }

    public static socket: WebSocket;

    public static async sendMusicData(): Promise<any> {
        let me = MyClass;
        let songData = me.findYouTubePlayerObject().getVideoData();
        let playerState = me.findYouTubePlayerObject().getPlayerState()
        return me.putMusicData(songData.title, songData.author, playerState);
    }

    public static connectWebSocket() {
        let me = MyClass;
        // IMPORTANT: Use 'ws://' for HTTP and 'wss://' for HTTPS
        // If your client and server are on the same domain, you can use relative paths.
        // Example: const socket = new WebSocket('ws://localhost:5000/ws');
        // Or if running on a different port/domain:
        me.socket = new WebSocket('ws://127.0.0.1:9191/ws'); // Replace [YourServerPort]

        me.socket.onopen = (event) => {
            console.log('WebSocket connection opened:', event);
            // You can send an initial message to the server if needed
            // socket.send('Hello from client!');
        };

        me.socket.onmessage = (event) => {

            me.handleMusicControlEvent(event);

        };

        me.socket.onclose = (event) => {
            console.log('WebSocket connection closed:', event);
            if (event.wasClean) {
                console.log(`Connection closed cleanly, code=${event.code}, reason=${event.reason}`);
            } else {
                // e.g. server process killed or network down
                console.error('Connection died');
                // Optionally try to reconnect
                setTimeout(me.connectWebSocket, 1000);
            }
        };

        me.socket.onerror = (error) => {
            console.error('WebSocket error:', error);
        };

        //setTimeout(function () {
        //    me.socket.close();
        //    setTimeout(me.connectWebSocket, 1000);
        //}, 60000);
    }

    public static watchForSongChangesAndSend() {
        let me = MyClass;

        me.connectWebSocket();

        let songTitleElement = document.querySelector("#layout > ytmusic-player-bar > div.middle-controls.style-scope.ytmusic-player-bar > div.content-info-wrapper.style-scope.ytmusic-player-bar > yt-formatted-string") as HTMLHtmlElement

        let playerThing = document.querySelector("#play-pause-button #button");

        if (playerThing) {
            playerThing.addEventListener("click", function () {
                setTimeout(function () {
                    let songData = me.findYouTubePlayerObject().getVideoData();
                    let playerState = me.findYouTubePlayerObject().getPlayerState()
                    me.putMusicData(songData.title, songData.author, playerState);
                }, 100);
            });
        }

        if (songTitleElement) {
            const observer = new MutationObserver((mutationsList, observer) => {
                for (const mutation of mutationsList) {
                    if (mutation.type === 'characterData' || mutation.type === 'childList') {

                        let songData = me.findYouTubePlayerObject().getVideoData();
                        let playerState = me.findYouTubePlayerObject().getPlayerState()
                        me.putMusicData(songData.title, songData.author, playerState);
                    }
                }
            });

            observer.observe(songTitleElement, {
                childList: true,
                subtree: true,
                characterData: true
            });
        }


        async function fallBack(lastSong?: string) {
            let songData = me.findYouTubePlayerObject().getVideoData();
            let playerState = me.findYouTubePlayerObject().getPlayerState();

            try {
                await me.putMusicData(songData.title, songData.author, playerState);
            } catch (ex) {
                console.error("Error getting song data", ex);
            }

            setTimeout(async function () {
                await fallBack(lastSong);
            }, 5000);
        }

        fallBack();

    }

    public static async sendServerMessage(action: string, data: any): Promise<any> {
        let me = this;

        const response: Response = await fetch(`http://127.0.0.1:9191/mini-monitor/?action=${action}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });

        return await response.json();


    }


    public static async putMusicData(title: string, artist: string, playerState: number): Promise<any> {
        let me = this;

        let musicData = {
            DataType: "MusicData",
            Title: title,
            Artist: artist,
            PlayerState: playerState
        }

        /*
        public class MusicData
        {
            public string DataType { get; set; } = "MusicData";
            public bool Success { get; set; }
            public string? Title { get; set; }
            public string? Artist { get; set; }
            public string? Album { get; set; }
            public string? AlbumArtUrl { get; set; }
            public string? Error { get; set; }
        }

        */

        console.log("putMusicData", musicData);

        const response: Response = await fetch('http://127.0.0.1:9191/mini-monitor/?action=putMusicData', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(musicData)
        });

        return await response.json();


    }


    // Static variables for managing the meeting flash effect
    static meetingFlashIntervalRef = 0;
    static lastMeetingFlashState = false;


    public static async FlashElement(element: HTMLElement, durationMs: number = Infinity, rateMs: number = 500) {
        if (!element) return;

        let running = true;
        const endTime = Date.now() + durationMs;

        while (running && Date.now() < endTime) {
            element.style.opacity = element.style.opacity === "0.5" ? "1" : "0.5";
            await new Promise(resolve => setTimeout(resolve, rateMs));
        }

        // Ensure final state is visible
        element.style.opacity = "1";
    }

    public static async UpdateSensorDataForB(test?: boolean) {
        let me = this;

        try {

            const response: Response = await fetch('http://127.0.0.1:9191/mini-monitor/?action=sensorData');

            const responseData: any = await response.json();

            if (responseData.NoData !== true) {




                const sensorData = responseData["sensorData"];
                const calendarData = responseData["calendarData"];
                const weatherData = responseData["weatherData"];
                const musicData = responseData["musicData"];

                if (musicData && musicData.DataType === "MusicData") {

                    me.trySetInnerText("song-title", musicData.Title);
                    me.trySetInnerText("artist-name", musicData.Artist);
                    me.trySetInnerText("song-album", musicData.Album);

                    //if (musicData.Title) {
                    //    $("#btn-yt-open").hide();
                    //}

                    /*
                    
                        UNSTARTED = -1,
                        ENDED = 0,
                        PLAYING = 1,
                        PAUSED = 2,
                        BUFFERING = 3,
                        CUED = 5

                    */
                    if (musicData.PlayerState === 2) {
                        me.trySetInnerText("btn-yt-playpause", "Play");
                    } else {
                        me.trySetInnerText("btn-yt-playpause", "Pause");
                    }

                    // 
                }


                if (weatherData && weatherData.DataType === "WeatherData") {

                    //console.log("weatherData", weatherData)
                    me.trySetInnerText("weather-text-temp", weatherData.Temperature);


                    let weather_condition = me.weather_conditions.find((x: any) => x.description === weatherData.Description);
                    if (weather_condition) {

                        let iconUrl = `http://openweathermap.org/img/wn/${weather_condition.icon}@2x.png`;
                        (document.getElementById("weather-icon") as HTMLImageElement).src = iconUrl;
                        me.trySetInnerText("weather-text-condition", weather_condition?.short_name);
                    }



                    //let relativeTimeFromNow = luxon.DateTime.fromISO(calendarData.StartTimeUtc).toRelative({ base: luxon.DateTime.now(), style: 'long' });
                    //me.trySetInnerText("meeting-time-relative", relativeTimeFromNow);

                    // 
                    // meeting-title
                    // 
                    // Summary
                    //HasEvents
                }

                if (calendarData && calendarData.DataType === "CalendarData" && calendarData.HasEvents) {
                    me.trySetInnerText("meeting-title", calendarData.Summary);

                    let relativeTimeFromNow = luxon.DateTime.fromISO(calendarData.StartTimeUtc).toRelative({ base: luxon.DateTime.now(), style: 'long' });
                    me.trySetInnerText("meeting-time-relative", relativeTimeFromNow);

                    // Calculate minutes until meeting for flash logic
                    let minutesUntil = (luxon.DateTime.fromISO(calendarData.StartTimeUtc).toMillis() - luxon.DateTime.now().toMillis()) / (1000 * 60);

                    let gridIitemHeaderLeft = (document.querySelector(".grid-item.header-left") as HTMLDivElement);

                    // Handle flashing for meeting-time-relative when less than 5 minutes
                    gridIitemHeaderLeft.style.backgroundColor = "";


                    if (minutesUntil <= 1 && minutesUntil > 0) {

                        gridIitemHeaderLeft.style.backgroundColor = "crimson";


                        // Start flashing if not already flashing
                        if (!me.lastMeetingFlashState) {
                            me.lastMeetingFlashState = true;
                            if (me.meetingFlashIntervalRef) {
                                clearInterval(me.meetingFlashIntervalRef);
                            }
                            me.meetingFlashIntervalRef = setInterval(async () => {
                                const element = document.getElementById("meeting-time-relative") as HTMLElement;
                                await MyClass.FlashElement(element, 500, 100);
                            }, 1000);
                        }
                    } else if (minutesUntil <= 5 && minutesUntil > 0) {
                        // Start flashing if not already flashing
                        if (!me.lastMeetingFlashState) {
                            me.lastMeetingFlashState = true;
                            if (me.meetingFlashIntervalRef) {
                                clearInterval(me.meetingFlashIntervalRef);
                            }
                            me.meetingFlashIntervalRef = setInterval(async () => {
                                const element = document.getElementById("meeting-time-relative") as HTMLElement;
                                await MyClass.FlashElement(element, 500, 100);
                            }, 2000);
                        }
                    } else if (minutesUntil <= 10 && minutesUntil > 0) {
                        // Start flashing if not already flashing
                        if (!me.lastMeetingFlashState) {
                            me.lastMeetingFlashState = true;
                            if (me.meetingFlashIntervalRef) {
                                clearInterval(me.meetingFlashIntervalRef);
                            }
                            me.meetingFlashIntervalRef = setInterval(async () => {
                                const element = document.getElementById("meeting-time-relative") as HTMLElement;
                                await MyClass.FlashElement(element, 1000, 100);
                            }, 10000);
                        }
                    } else {
                        // Stop flashing if meeting is more than 5 minutes away or has started
                        if (me.lastMeetingFlashState) {
                            me.lastMeetingFlashState = false;
                            if (me.meetingFlashIntervalRef) {
                                clearInterval(me.meetingFlashIntervalRef);
                                me.meetingFlashIntervalRef = 0;
                            }
                            // Ensure element is visible when stopping flash
                            const element = document.getElementById("meeting-time-relative");
                            if (element) {
                                element.style.opacity = "1";
                            }
                        }
                    }

                    if (calendarData.StartTimeUtc) {
                        me.trySetInnerText("meeting-details", luxon.DateTime.fromISO(calendarData.StartTimeUtc).toFormat('yyyy-MM-dd hh:mm a'));
                    } else {
                        me.trySetInnerText("meeting-details", "TBD");
                    }

                    //

                    // 
                    // meeting-title
                    // 
                    // Summary
                    //HasEvents
                } else {
                    // No meeting data - stop any flashing
                    if (me.lastMeetingFlashState) {
                        me.lastMeetingFlashState = false;
                        if (me.meetingFlashIntervalRef) {
                            clearInterval(me.meetingFlashIntervalRef);
                            me.meetingFlashIntervalRef = 0;
                        }
                    }

                    me.trySetInnerText("meeting-title", "Nothing soon!");
                    me.trySetInnerHTML("meeting-time-relative", "&nbsp;");
                    me.trySetInnerHTML("meeting-details", "&nbsp;");

                    // Ensure meeting-time-relative is visible when no meeting
                    const element = document.getElementById("meeting-time-relative");
                    if (element) {
                        element.style.opacity = "1";
                    }
                }

                if (sensorData && sensorData.DataType === "SensorData") {

                    (document.getElementById("gpu-usage") as HTMLDivElement).innerText = `${sensorData.gpuLoad}%`;

                    // cpu-usage
                    (document.getElementById("cpu-usage") as HTMLDivElement).innerText = `${Math.round(sensorData.cpuTotal)}%`;
                }
            }
            setTimeout(async function () { await MyClass.UpdateSensorDataForB(); }, 1000);


        } catch (ex) {
            console.error(ex)
            setTimeout(async function () { await MyClass.UpdateSensorDataForB(); }, 5000);
        }
    }

    public static async getPlaylists(): Promise<any[] | null> {
        try {
            const response: Response = await fetch('http://localhost:8000/getPlaylists');
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            const playlists = await response.json();
            return playlists;
        } catch (error) {
            console.error('Error fetching playlists:', error);
            return null;
        }
    }


    public static async getPlaylistTracks(playlistId: string): Promise<any[] | null> {
        try {
            const url = `http://localhost:8000/getPlaylistTracks?playlistId=${encodeURIComponent(playlistId)}`;
            const response: Response = await fetch(url);
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            const tracks = await response.json();
            return tracks;
        } catch (error) {
            console.error('Error fetching playlist tracks:', error);
            return null;
        }
    }

    private static toProperCase(str: string): string {
        return str.replace(/\w\S*/g, (txt) => txt.charAt(0).toUpperCase() + txt.substr(1).toLowerCase());
    }

    public static async UpdateSensorData() {
        let me = this;


        try {
            const response: Response = await fetch('http://localhost/mini-monitor-data/');
            const responseData: string = await response.text();
            let data = JSON.parse(responseData);
            me.trySetInnerText("cpuData", Math.round(+data.cpuTotal).toString());

        } catch (ex) {
            console.error(ex)
        }

        setTimeout(function () { me.UpdateSensorData() }, 1000);
    }

    public static Close() {
        let me = this;

        me.external.sendMessage('Close');
    }

    public static ToggleYTM() {
        let me = this;

        //if (me.ytm == null) {
        //    //me.ytm = window.open("https://music.youtube.com/");
        //    me.external.sendMessage('FindYTM');
        //} else {
        //    me.external.sendMessage('ToggleYTM');
        //}
        me.external.sendMessage('ToggleYTM');

        //me.external.sendMessage('TestWebDriver');


        //me.external.sendMessage('SendTest');
    }

    public static SendMessage(message: string) {
        let me = this;

        me.external.sendMessage(message);
    }

    public static UpdateCalendar() {
        let me = this;
        MyClass.setStyleDisplay("nextMeeting", "none");
        if (me.lastCalendarInterval) {
            clearInterval(me.lastCalendarInterval);
        }
        me.external.sendMessage("UpdateCalendar");
    }

    public static PlayPause() {
        let me = this;

        me.external.sendMessage('PlayPause');
    }

    public static doX(): string {

        alert("doX1");
        return "X";
    }


    public static closeMe() {
        window.close();
    }
}

(window as any)["Util"] = Util;

//var myYTMHelper = new YTMHelper();

//function onYouTubeIframeAPIReady() {
//    myYTMHelper.onYouTubeIframeAPIReady();
//}

if (MyClass.isPageB()) {
    MyClass.UpdateSensorDataForB();
}


var _YTMusicSite = null as Window | null;
function openYTMusicSite(btn: HTMLButtonElement) {

    MyClass.sendServerMessage("wireUpYT", { "action": "na" });

    //// Store the current innerText in dataset
    //btn.dataset.prevInnerText = btn.innerText;
    //btn.innerText = "Clicked";
    //btn.disabled = true;

    //setTimeout(() => {
    //    btn.innerText = btn.dataset.prevInnerText as string;
    //    btn.disabled = false;
    //}, 1000);

    //if (_YTMusicSite && !_YTMusicSite.closed) {
    //    _YTMusicSite.close();
    //} else {
    //    _YTMusicSite = window.open('https://music.youtube.com/', 'myytmusic', 'width=1200,height=800');
    //}
}


//alert(MyClass.isPageB());

