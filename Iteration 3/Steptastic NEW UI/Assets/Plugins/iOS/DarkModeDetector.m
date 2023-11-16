//
//  DarkModeDetector.m
//  DarkModeDetector
//
//  Created by Tom Redway on 20/10/2023.
//

#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>

BOOL IsDarkModeEnabled(void) {
    if (@available(iOS 13.0, *)) {
        UIUserInterfaceStyle currentStyle = UITraitCollection.currentTraitCollection.userInterfaceStyle;
        return currentStyle == UIUserInterfaceStyleDark;
    } else {
        return NO;
    }
}
