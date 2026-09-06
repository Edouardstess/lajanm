// Fichier de build racine : les plugins y sont déclarés sans être appliqués,
// puis appliqués dans le module :app.
plugins {
    alias(libs.plugins.android.application) apply false
    alias(libs.plugins.kotlin.android) apply false
    alias(libs.plugins.ksp) apply false
    // Déclaré ici pour être disponible, appliqué conditionnellement dans
    // :app selon la présence de google-services.json (voir app/build.gradle.kts).
    alias(libs.plugins.google.services) apply false
}
