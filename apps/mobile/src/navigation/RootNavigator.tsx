import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import React from 'react';
import { ActivityIndicator, View } from 'react-native';
import { useAuth } from '../context/AuthContext';
import { useTranslation } from '../i18n';
import { DevicesScreen } from '../screens/DevicesScreen';
import { HistoryScreen } from '../screens/HistoryScreen';
import { HomeScreen } from '../screens/HomeScreen';
import { KycCaptureScreen } from '../screens/KycCaptureScreen';
import { LoginScreen } from '../screens/LoginScreen';
import { NewTicketScreen } from '../screens/NewTicketScreen';
import { PayoutScreen } from '../screens/PayoutScreen';
import { ProfileScreen } from '../screens/ProfileScreen';
import { RegisterScreen } from '../screens/RegisterScreen';
import { SupportScreen } from '../screens/SupportScreen';
import { TicketThreadScreen } from '../screens/TicketThreadScreen';
import { TopupScreen } from '../screens/TopupScreen';
import { TransferScreen } from '../screens/TransferScreen';
import { AppLockGate } from '../security/AppLockGate';
import { colors } from '../theme';

const Stack = createNativeStackNavigator();

function AuthStack() {
  return (
    <Stack.Navigator screenOptions={{ headerShown: false }}>
      <Stack.Screen name="Login" component={LoginScreen} />
      <Stack.Screen name="Register" component={RegisterScreen} />
    </Stack.Navigator>
  );
}

function AppStack() {
  const { t } = useTranslation();
  return (
    <Stack.Navigator>
      <Stack.Screen name="Home" component={HomeScreen} options={{ title: 'Lajan’m' }} />
      <Stack.Screen name="Transfer" component={TransferScreen} options={{ title: t('wallet.transfer_title') }} />
      <Stack.Screen name="History" component={HistoryScreen} options={{ title: t('wallet.history_title') }} />
      <Stack.Screen name="Topup" component={TopupScreen} options={{ title: t('topup.title') }} />
      <Stack.Screen name="Payout" component={PayoutScreen} options={{ title: t('payout.title') }} />
      <Stack.Screen name="Kyc" component={KycCaptureScreen} options={{ title: t('kyc.title') }} />
      <Stack.Screen name="Profile" component={ProfileScreen} options={{ title: t('profile.title') }} />
      <Stack.Screen
        name="Devices"
        component={DevicesScreen}
        options={{ title: t('profile.devices_title') }}
      />
      <Stack.Screen name="Support" component={SupportScreen} options={{ title: t('support.title') }} />
      <Stack.Screen
        name="SupportNewTicket"
        component={NewTicketScreen}
        options={{ title: t('support.new_ticket') }}
      />
      <Stack.Screen
        name="SupportTicket"
        component={TicketThreadScreen}
        options={{ title: t('support.tickets_title') }}
      />
    </Stack.Navigator>
  );
}

export function RootNavigator() {
  const { user, isReady } = useAuth();

  if (!isReady) {
    return (
      <View style={{ flex: 1, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.background }}>
        <ActivityIndicator color={colors.primary} />
      </View>
    );
  }

  return (
    <NavigationContainer>
      {user ? (
        <AppLockGate>
          <AppStack />
        </AppLockGate>
      ) : (
        <AuthStack />
      )}
    </NavigationContainer>
  );
}
