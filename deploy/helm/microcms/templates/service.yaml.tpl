apiVersion: v1
kind: Service
metadata:
  name: {{ include "microcms.fullname" . }}
  labels:
    {{- include "microcms.labels" . | nindent 4 }}
spec:
  type: {{ .Values.service.type }}
  ports:
    - port: {{ .Values.service.port }}
      targetPort: {{ .Values.service.targetPort }}
      protocol: TCP
      name: http
  selector:
    {{- include "microcms.selectorLabels" . | nindent 4 }}
