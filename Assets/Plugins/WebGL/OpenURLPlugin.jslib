mergeInto(LibraryManager.library, {  
    OpenUrlInSameTab: function (urlPtr) {  
        var url = UTF8ToString(urlPtr);  
          
        // ブラウザネイティブの確認ダイアログを表示  
        var result = confirm("LINEクーポン画面に移動します。よろしいですか？");  
        if (result) {  
            // OKが押されたら親ウィンドウごと移動  
            window.top.location.href = url;  
        }  
    }  
});  
