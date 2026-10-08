mergeInto(LibraryManager.library, {
    InitGyroAndRequestPermission: function (objectNamePtr, callbackNamePtr) {
        var objectName = UTF8ToString(objectNamePtr);
        var callbackName = UTF8ToString(callbackNamePtr);

        function handleOrientation(event) {
            // beta (前後の傾き) と gamma (左右の傾き) をカンマ区切りで送信
            var beta = event.beta || 0;
            var gamma = event.gamma || 0;
            var dataString = beta + "," + gamma;
            
            SendMessage(objectName, callbackName, dataString);
        }

        if (typeof DeviceOrientationEvent !== 'undefined' && typeof DeviceOrientationEvent.requestPermission === 'function') {
            DeviceOrientationEvent.requestPermission()
                .then(function (response) {
                    if (response === 'granted') {
                        window.addEventListener('deviceorientation', handleOrientation);
                    }
                })
                .catch(console.error);
        } else {
            window.addEventListener('deviceorientation', handleOrientation);
        }
    }
});
