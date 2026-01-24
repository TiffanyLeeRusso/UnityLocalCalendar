package com.BoxCatGames;

import android.app.Fragment;
import android.content.Intent;
import android.os.Bundle;
import java.lang.reflect.Method;

public class PickerHelper extends Fragment {
    private static final int REQUEST_CODE = 4242;
    private boolean mPickerOpened = false;

    public static void launchPicker(android.app.Activity activity) {
        PickerHelper fragment = new PickerHelper();
        activity.getFragmentManager().beginTransaction().add(fragment, "PickerFragment").commit();
    }

    @Override
    public void onStart() {
        super.onStart();

        // Only launch the intent once per fragment instance
        if (!mPickerOpened) {
            mPickerOpened = true;
            Intent intent = new Intent(Intent.ACTION_OPEN_DOCUMENT);
            intent.addCategory(Intent.CATEGORY_OPENABLE);
            intent.setType("*/*");
            startActivityForResult(intent, REQUEST_CODE);
        }
    }

    @Override
    public void onActivityResult(int requestCode, int resultCode, Intent data) {
        if (requestCode == REQUEST_CODE) {
            // resultCode -1 is Activity.RESULT_OK
            if (resultCode == -1 && data != null && data.getData() != null) {
                String uriString = data.getData().toString();
                //getContentResolver().takePersistableUriPermission(data.getData(), Intent.FLAG_GRANT_READ_URI_PERMISSION);
                unitySendMessage("AppBootstrap", "OnReceiveUri", uriString);
            }
            
            // Cleanup: remove the fragment
            getFragmentManager().beginTransaction().remove(this).commit();
        }
    }

    private void unitySendMessage(String gameObject, String method, String message) {
        try {
            Class<?> unityPlayerClass = Class.forName("com.unity3d.player.UnityPlayer");
            Method sendMessageMethod = unityPlayerClass.getMethod("UnitySendMessage", 
                String.class, String.class, String.class);
            sendMessageMethod.invoke(null, gameObject, method, message);
        } catch (Exception e) {
            // If this fails, it's usually because the GameObject name is wrong
            e.printStackTrace();
        }
    }
}
