"use strict";
class MyClass {
    static ytm = null;
    static external = window["external"];
    static Close() {
        let me = this;
        me.external.sendMessage('Close');
    }
    static StartYTM() {
        let me = this;
        if (me.ytm == null) {
            me.ytm = window.open("https://music.youtube.com/");
            me.external.sendMessage('FindYTM');
        }
        else {
            me.external.sendMessage('ShowYTM');
        }
        //me.external.sendMessage('SendTest');
    }
    static SendMessage(message) {
        let me = this;
        me.external.sendMessage(message);
    }
    static PlayPause() {
        let me = this;
        me.external.sendMessage('PlayPause');
    }
    static doX() {
        alert("doX1");
        return "X";
    }
}
