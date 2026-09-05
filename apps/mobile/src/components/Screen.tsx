import React from 'react';
import { ScrollView, StyleSheet, Text, View, ViewStyle } from 'react-native';
import { colors, fonts, spacing, typography } from '../theme';

interface Props {
  children: React.ReactNode;
  /** Titre de l'écran, quand la barre de navigation n'en porte pas déjà un. */
  title?: string;
  subtitle?: string;
  /** Un formulaire court est centré ; une liste part du haut. */
  center?: boolean;
  scroll?: boolean;
  /** Zone collée en bas : l'action principale reste sous le pouce. */
  footer?: React.ReactNode;
  contentStyle?: ViewStyle;
}

/**
 * Ossature commune à tous les écrans : fond, marges, et surtout la
 * séparation entre le contenu qui défile et l'action principale, qui
 * elle ne défile pas. Sur un formulaire de paiement, un bouton « Voye »
 * qu'il faut aller chercher en faisant défiler est un bouton qu'on rate.
 */
export function Screen({ children, title, subtitle, center, scroll, footer, contentStyle }: Props) {
  const body = (
    <View style={[styles.content, center && styles.centered, contentStyle]}>
      {title && <Text style={styles.title}>{title}</Text>}
      {subtitle && <Text style={styles.subtitle}>{subtitle}</Text>}
      {children}
    </View>
  );

  return (
    <View style={styles.root}>
      {scroll ? (
        <ScrollView
          style={styles.flex}
          contentContainerStyle={styles.scrollContent}
          keyboardShouldPersistTaps="handled"
        >
          {body}
        </ScrollView>
      ) : (
        <View style={styles.flex}>{body}</View>
      )}
      {footer && <View style={styles.footer}>{footer}</View>}
    </View>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: colors.ground },
  flex: { flex: 1 },
  scrollContent: { flexGrow: 1 },
  content: { flex: 1, padding: spacing.lg },
  centered: { justifyContent: 'center' },
  title: {
    fontSize: typography.title,
    fontFamily: fonts.bold,
    color: colors.text,
    letterSpacing: -0.3,
    marginBottom: spacing.xs,
  },
  subtitle: {
    fontSize: typography.label, fontFamily: fonts.regular,
    color: colors.muted,
    marginBottom: spacing.md,
    lineHeight: 21,
  },
  footer: {
    paddingHorizontal: spacing.lg,
    paddingTop: spacing.md,
    paddingBottom: spacing.lg,
  },
});
